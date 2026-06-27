using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class LevelUpEffectView : MonoBehaviour, ISpeedChange
{
    public float TimeScale { get; set; } = 1f;

    /// <summary>
    /// プレイヤーのステータスアップに応じたエフェクトを再生する
    /// </summary>
    public void Play(StatSkillType statType)
    {
        if (_vfx == null)
        {
            Debug.LogError("[LevelUpEffectView] VisualEffect is missing.", this);
            return;
        }

        _effectQueue.Enqueue(statType);

        if (_isPlaying)
        {
            return;
        }

        _isPlaying = true;

        ProcessQueue().Forget();
    }

    public void OnSpeedChange(float scale)
    {
        TimeScale = scale;

        if (_vfx == null) return;

        // VFXの再生速度を変更
        _vfx.playRate = TimeScale;
    }

    [SerializeField] private VisualEffect _vfx;

    [SerializeField]
    private LevelUpEffectData[] _effectDataArray =
        new LevelUpEffectData[5];

    [SerializeField]
    private float _effectDuration = 1.5f;

    private Dictionary<StatSkillType, LevelUpEffectData> _effectMap;

    private Queue<StatSkillType> _effectQueue =
        new Queue<StatSkillType>();

    private bool _isPlaying;

    private static readonly int SymbolTextureID =
        Shader.PropertyToID("SymbolTexture");

    private static readonly int SecondaryColorID =
        Shader.PropertyToID("SecondaryColor");

    private static readonly int LifeColorID =
        Shader.PropertyToID("ColorOverLife");

    private static readonly int HitEventID =
        Shader.PropertyToID("hit");

    private void Awake()
    {
        if (_vfx == null) _vfx = GetComponent<VisualEffect>();

        if (_vfx == null)
        {
            Debug.LogError("[LevelUpEffectView] VisualEffect is missing.", this);
        }

        _effectMap = new Dictionary<StatSkillType, LevelUpEffectData>();

        if (_effectDataArray != null)
        {
            foreach (var data in _effectDataArray)
            {
                _effectMap[data.StatSkillType] = data;
            }
        }

        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
        {
            // プレイヤーにくっついているエフェクトなので、両方のグループに登録しておく
            hitStopManager.Register(this, HitStopTargetGroup.Effects);
            hitStopManager.Register(this, HitStopTargetGroup.Player);
        }
    }

    /// <summary>
    /// エフェクトの再生をキューで管理する。複数のステータスアップが同時に発生した場合でも、順番にエフェクトを再生できるようにするため。
    /// </summary>
    /// <returns></returns>
    private async UniTask ProcessQueue()
    {
        while (_effectQueue.Count > 0)
        {
            var statType = _effectQueue.Dequeue();

            PlayInternal(statType);

            await UniTask.WaitForSeconds(
                _effectDuration,
                cancellationToken: destroyCancellationToken);
        }

        _isPlaying = false;
    }

    /// <summary>
    /// 実際にエフェクトを再生する処理。StatSkillTypeに応じて、VFXのパラメータを設定してから再生する。
    /// </summary>
    /// <param name="statType"></param>
    private void PlayInternal(StatSkillType statType)
    {
        if (_vfx == null) return;

        if (_effectMap.TryGetValue(statType, out var data) == false)
        {
            Debug.LogWarning($"[LevelUpEffectView] Missing settings for {statType}.", this);
            return;
        }

        _vfx.SetTexture(SymbolTextureID, data.SymbolTexture);
        _vfx.SetVector4(SecondaryColorID, data.SecondaryColor);
        _vfx.SetGradient(LifeColorID, data.ColorOverLife);
        _vfx.SendEvent(HitEventID);
    }
}

[Serializable]
public struct LevelUpEffectData
{
    public StatSkillType StatSkillType => _statSkillType;
    public Texture2D SymbolTexture => _symbolTexture;
    public Color SecondaryColor => _secondaryColor;
    public Gradient ColorOverLife => _colorOverLife;

    [SerializeField] private StatSkillType _statSkillType;

    [SerializeField] private Texture2D _symbolTexture;

    [ColorUsage(true, true)]
    [SerializeField] private Color _secondaryColor;

    [GradientUsage(true)]
    [SerializeField] private Gradient _colorOverLife;
}
