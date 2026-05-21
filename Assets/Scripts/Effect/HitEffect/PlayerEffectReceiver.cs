using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//カメラのPostProcessingをONにしないとVolumeエフェクトが動作しないです。

/// <summary>
/// プレイヤーの被ダメージエフェクトを受け取るクラス
/// </summary>
public class PlayerEffectReceiver : MonoBehaviour
{
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

        if(_effectManager == null)
        {
            Debug.LogError("EffectManager が見つかりません", this);
            enabled = false;
            return;
        }

        if (_volume != null && _volume.profile.TryGet(out _vignette))
        {
            _vignette.intensity.value = 0f;
        }

        _player.OnDamagedEffect += HandleDamaged;
    }

    private Player _player;
    private EffectManager _effectManager;
    private Vignette _vignette;
    /// <summary>
    /// フラッシュエフェクトのキャンセルトークンソース
    /// </summary>
    private CancellationTokenSource _flashCts;

    [Header("Damage Effects"), Tooltip("ダメージ時のエフェクトID")]
    [SerializeField] private string _damageEffectKey = "player_hit";

    [Header("Screen Effects"), Tooltip("画面エフェクト")]
    [SerializeField] private Volume _volume;
    [SerializeField, Tooltip("消えるまでの時間")] private float _flashDuration = 2.5f;
    [SerializeField, Tooltip("フラッシュの長さ")] private float _flashAlpha = 0.5f;

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnDamagedEffect -= HandleDamaged;
        }
        _flashCts?.Cancel();
        _flashCts?.Dispose();
    }

    /// <summary>
    /// プレイヤーの被ダメージエフェクトを処理するメソッド
    /// </summary>
    /// <param name="context"></param>
    private void HandleDamaged(PlayerDamageEffectContext context)
    {
        if(_effectManager == null) return;

        _effectManager.PlayEffect(
            _damageEffectKey,
            context.HitPosition);

        PlayerDamageFlash().Forget();
    }

    /// <summary>
    /// ダメージのフラッシュ画面エフェクトを処理するメソッド
    /// </summary>
    /// <returns></returns>
    private async UniTaskVoid PlayerDamageFlash()
    {
        if (_vignette == null) return;


        _flashCts?.Cancel();
        _flashCts?.Dispose();

        _flashCts = new CancellationTokenSource();

        CancellationToken token = _flashCts.Token;

        //フラッシュエフェクトの初期化
        _vignette.intensity.value = _flashAlpha;

        try
        {
            //フラッシュエフェクトの更新
            while (_vignette.intensity.value > 0f)
            {
                _vignette.intensity.value -= Time.deltaTime / _flashDuration * _flashAlpha;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (_volume != null && _volume.profile.TryGet(out _vignette))
            {
                //最終の値の保存
                _vignette.intensity.value = 0f;
            }
            else Debug.LogWarning("Vignette が見つかりません", this);
        }
        catch (OperationCanceledException)
        {
            // 被弾時に前回のフラッシュを停止したため
        }
    }
}
