using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// 各武器纏エフェクトにつけてください
/// </summary>
public class WeaponEffectView : MonoBehaviour, IWeaponEffect, ISpeedChange
{
    // プロパティ実装
    public float TimeScale
    {
        get => _timeScale;
        set
        {
            _timeScale = value;
            OnSpeedChange(_timeScale);
        }
    }

    //開始時の処理
    public void Play()
    {
        gameObject.SetActive(true);
        OnSpeedChange(_timeScale);
    }

    // エフェクト停止時の処理
    public void Stop()
    {
        gameObject.SetActive(false);
    }

    public void OnSpeedChange(float scale)
    {
        if (_vfx == null) return;

        _vfx.playRate = scale;
    }

    private float _timeScale = 1f;
    [SerializeField] private VisualEffect _vfx;

    private void Awake()
    {
        if (_vfx == null) _vfx = GetComponent<VisualEffect>();

        if (_vfx == null)
        {
            Debug.LogError("[WeaponEffectView] VisualEffect is missing.", this);
        }
    }
}
