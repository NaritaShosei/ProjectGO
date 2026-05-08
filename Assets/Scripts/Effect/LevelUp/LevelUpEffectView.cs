using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class LevelUpEffectView : MonoBehaviour, ISpeedChange
{
    public float TimeScale { get; set; } = 1f;

    public void Play(StatSkillType statType)
    {
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
        _effectMap = new Dictionary<StatSkillType, LevelUpEffectData>();

        foreach (var data in _effectDataArray)
        {
            _effectMap[data.StatSkillType] = data;
        }
    }

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

    private void PlayInternal(StatSkillType statType)
    {
        if (_effectMap.TryGetValue(statType, out var data) == false)
        {
            Debug.LogWarning(
                $"[LevelUpEffectView] {statType} の設定がありません。");

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
