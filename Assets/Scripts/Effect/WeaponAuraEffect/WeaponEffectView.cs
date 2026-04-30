using UnityEngine;
using UnityEngine.VFX;

public class WeaponEffectView : MonoBehaviour, IWeaponEffect, ISpeedChange
{
    private float _timeScale = 1f;
    [SerializeField] private VisualEffect _vfx;

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

        _vfx.playRate = scale;
    }
}
