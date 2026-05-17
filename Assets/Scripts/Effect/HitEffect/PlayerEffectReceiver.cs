using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//カメラのPostProcessingをONにしないとVolume使えないです。


public class PlayerEffectReceiver : MonoBehaviour
{
    private Player _player;
    private EffectManager _effectManager;
    private Vignette _vignette;
    private CancellationTokenSource _flashCts;

    [Header("Damage Effects"), Tooltip("ダメージ時のエフェクトID")]
    [SerializeField] private string _damageEffectKey = "player_hit";

    [Header("Screen Effects"), Tooltip("画面エフェクト")]
    [SerializeField] private Volume _volume;
    [SerializeField] private float _flashDuration = 2.5f;
    [SerializeField] private float _flashAlpha = 0.5f;


    public void Init(Player player, EffectManager effectManager)
    {
        _player = player;
        _effectManager = effectManager;

        if (_player == null)
        {
            Debug.LogError("Player が見つかりません", this);
            enabled = false;
            return;
        }

        if (_volume != null && _volume.profile.TryGet(out _vignette))
        {
            _vignette.intensity.value = 0f;
        }

        _player.OnDamagedEffect += HandleDamaged;
    }

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnDamagedEffect -= HandleDamaged;
        }
        _flashCts?.Cancel();
        _flashCts?.Dispose();
    }

    private void HandleDamaged(PlayerDamageEffectContext context)
    {
        _effectManager.PlayEffect(
            _damageEffectKey,
            context.HitPosition);

        PlayerDamageFlash().Forget();
    }

    private async UniTaskVoid PlayerDamageFlash()
    {
        if (_volume == null) return;

        _flashCts?.Cancel();
        _flashCts?.Dispose();
        _flashCts = new CancellationTokenSource();

        CancellationToken token = _flashCts.Token;

        _vignette.intensity.value = _flashAlpha;

        try
        {
            while (_vignette.intensity.value > 0f)
            {
                _vignette.intensity.value -= Time.deltaTime / _flashDuration * _flashAlpha;
                await UniTask.Yield(token);
            }

            _vignette.intensity.value = 0f;
        }
        catch (OperationCanceledException)
        {
        }   
    }
}
