using UnityEngine;
using UnityEngine.VFX;

public class ThunderEffectView : MonoBehaviour, ISpeedChange
{
    // ヒットストップ等で利用するタイムスケール
    public float TimeScale { get; set; } = 1f;

    /// <summary> プレイヤーのモードに応じたエフェクトの表示切り替え </summary>
    public void Play(PlayerMode mode)
    {
        switch (mode)
        {
            case PlayerMode.Thunder:
                Show();
                break;
            default:
                Hide();
                break;
        }
    }

    public void OnSpeedChange(float scale)
    {
        TimeScale = scale;
        if (_vfx == null) return;

        // VFXの再生速度を直接更新する
        _vfx.playRate = TimeScale;
    }

    [SerializeField] private VisualEffect _vfx;

    private void Awake()
    {
        if (_vfx == null) _vfx = GetComponent<VisualEffect>();

        if (_vfx == null)
        {
            Debug.LogError("[ThunderEffectView] VisualEffect is missing.", this);
        }
    }

    private void OnEnable()
    {
        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Register(this, HitStopTargetGroup.Effects);
        }
    }

    private void OnDisable()
    {
        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            hitStopManager.Unregister(this, HitStopTargetGroup.Effects);
        }
    }

    private void Show()
    {
        if (_vfx == null) return;
        // "create" イベントを送ることで、エフェクトを再生します [cite: 295]
        _vfx.SendEvent("create");
    }

    private void Hide()
    {
        if (_vfx == null) return;
        // "stop" イベントを送ることで、エフェクトを停止します [cite: 295]
        _vfx.SendEvent("stop");
    }
}
