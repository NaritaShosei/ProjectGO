using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>事前配置した4枚のImageを取得順に並べ替える。実行時にUIを生成しない。</summary>
public sealed class StatUpgradeIconView : MonoBehaviour
{
    public void BindManager(SkillManager manager)
    {
        UnbindManager();
        _manager = manager;
        if (_manager != null)
            _manager.OnApply += HandleStatApplied;
        RefreshIcons();
    }

    public void UnbindManager()
    {
        if (_manager != null)
            _manager.OnApply -= HandleStatApplied;
        _manager = null;
        ResetAnimations();
    }

    [Header("事前配置Image（攻撃力・HP・雷ゲージ・クリティカル率の順）")]
    [Tooltip("専用の親の直下に置いた4枚のImageを割り当てます。")]
    [SerializeField] private Image[] _icons = new Image[StatUpgradeState.StatCount];

    [Header("アイコン開放データ（開放レベルとSpriteの組）")]
    [SerializeField] private StatUpgradeIconData[] _attackIcons = CreateDefaultEntries();
    [SerializeField] private StatUpgradeIconData[] _healthIcons = CreateDefaultEntries();
    [SerializeField] private StatUpgradeIconData[] _thunderIcons = CreateDefaultEntries();
    [SerializeField] private StatUpgradeIconData[] _criticalIcons = CreateDefaultEntries();

    [Header("横一列の配置")]
    [SerializeField] private Vector2 _firstPosition = Vector2.zero;
    [Min(0f)] [SerializeField] private float _spacing = 150f;

    [Header("ポップ演出")]
    [SerializeField] private bool _enablePop = true;
    [Min(1f)] [SerializeField] private float _popScale = 1.3f;
    [Min(0f)] [SerializeField] private float _popDuration = 0.25f;
    [Tooltip("演出全体の時間のうち、拡大に使う割合。残りを縮小に使います。")]
    [Range(0.01f, 0.99f)] [SerializeField] private float _popExpandRatio = 0.35f;
    [SerializeField] private Ease _popExpandEase = Ease.OutQuad;
    [SerializeField] private Ease _popShrinkEase = Ease.InOutQuad;
    [SerializeField] private bool _useUnscaledTime = true;

    private readonly Vector3[] _baseScales = new Vector3[StatUpgradeState.StatCount];
    private readonly Tween[] _popTweens = new Tween[StatUpgradeState.StatCount];
    private SkillManager _manager;
    private bool _isInitialized;

    private void Awake()
    {
        InitializeIcons();
        RefreshIcons();
    }

    private void OnEnable()
    {
        RefreshIcons();
    }

    private void OnDisable()
    {
        ResetAnimations();
    }

    private void OnDestroy()
    {
        UnbindManager();
    }

    private void InitializeIcons()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        for (int i = 0; i < StatUpgradeState.StatCount; i++)
        {
            if (TryGetIcon(i, out Image icon))
                _baseScales[i] = icon.rectTransform.localScale;
        }
    }

    private void HandleStatApplied(StatSkillType type)
    {
        RefreshIcons();
        int index = StatUpgradeState.GetStatIndex(type);
        if (index < 0 || !isActiveAndEnabled || !TryGetIcon(index, out Image icon)) return;

        // 上限後や短時間の連続取得でも、元のサイズを基準に演出を再開する。
        PlayPopAnimation(index, icon);
    }

    private void PlayPopAnimation(int index, Image icon)
    {
        _popTweens[index]?.Kill();
        RectTransform target = icon.rectTransform;
        Vector3 baseScale = _baseScales[index];
        target.localScale = baseScale;
        if (!_enablePop || _popDuration <= 0f || !icon.isActiveAndEnabled) return;

        // AutoKillによる完了と中断の両方で元のサイズに戻し、連続取得で拡大が累積するのを防ぐ。
        float expandDuration = _popDuration * Mathf.Clamp(_popExpandRatio, 0.01f, 0.99f);
        _popTweens[index] = DOTween.Sequence()
            .Append(target.DOScale(baseScale * _popScale, expandDuration).SetEase(_popExpandEase))
            .Append(target.DOScale(baseScale, _popDuration - expandDuration).SetEase(_popShrinkEase))
            .SetEase(Ease.Linear)
            .SetUpdate(_useUnscaledTime)
            .SetAutoKill(true)
            .SetLink(icon.gameObject, LinkBehaviour.KillOnDisable)
            .OnKill(() =>
            {
                _popTweens[index] = null;
                if (target != null)
                    target.localScale = baseScale;
            });
    }

    private void RefreshIcons()
    {
        InitializeIcons();
        for (int i = 0; i < StatUpgradeState.StatCount; i++)
        {
            if (!TryGetIcon(i, out Image icon)) continue;
            int count = _manager == null ? 0 : _manager.StatUpgrades.GetAcquisitionCount(i);
            icon.gameObject.SetActive(count > 0);
            if (count == 0) continue;

            StatUpgradeIconData[] entries = GetIconEntries(i);
            int entryIndex = StatUpgradeIconData.GetUnlockedIndex(count, entries);
            icon.sprite = entryIndex >= 0 ? entries[entryIndex].IconSprite : null;
            // 未設定素材を白い矩形として表示しない。素材の割当後は通常表示する。
            icon.enabled = icon.sprite != null;
        }

        if (_manager == null) return;
        for (int position = 0; position < _manager.StatUpgrades.AcquiredCount; position++)
        {
            int index = _manager.StatUpgrades.GetOrderedIndex(position);
            if (!TryGetIcon(index, out Image icon)) continue;
            icon.rectTransform.SetSiblingIndex(position);
            icon.rectTransform.anchoredPosition = _firstPosition + Vector2.right * (_spacing * position);
        }
    }

    private StatUpgradeIconData[] GetIconEntries(int index)
    {
        return index switch
        {
            0 => _attackIcons,
            1 => _healthIcons,
            2 => _thunderIcons,
            3 => _criticalIcons,
            _ => null
        };
    }

    private bool TryGetIcon(int index, out Image icon)
    {
        icon = _icons != null && index < _icons.Length ? _icons[index] : null;
        return icon != null;
    }

    private void ResetAnimations()
    {
        if (!_isInitialized) return;
        for (int i = 0; i < StatUpgradeState.StatCount; i++)
        {
            _popTweens[i]?.Kill();
            _popTweens[i] = null;
            if (TryGetIcon(i, out Image icon))
                icon.rectTransform.localScale = _baseScales[i];
        }
    }

    private static StatUpgradeIconData[] CreateDefaultEntries()
    {
        return new[]
        {
            new StatUpgradeIconData(1, null),
            new StatUpgradeIconData(2, null),
            new StatUpgradeIconData(3, null),
            new StatUpgradeIconData(4, null),
            new StatUpgradeIconData(5, null)
        };
    }
}
