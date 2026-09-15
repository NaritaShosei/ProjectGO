using BlurShadersPro.BuiltIn;
using BlurShadersPro.URP;
using BossEnemy.Interface;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class ShoutSMB : BossCharacterSMB
{
    public void Init(
            Volume volume,
            IBossCharacterAnimationEventReceiver bossEnemyAnimationEventReceiver,
            IBossEnemyCharacterView bossEnemyCharacterView,
            Transform bossCharacterTransform)
    {
        _shoutVolume = volume;

        Init(bossEnemyAnimationEventReceiver, bossEnemyCharacterView, bossCharacterTransform);

        if (TryGetRadialBlur(out RadialBlurSettings radialBlurSettings))
        {
            Debug.Log("取得成功");
            _saveStrength.value = radialBlurSettings.strength.value;
            _saveStepSize.value = radialBlurSettings.stepSize.value;
        }
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _shoutVersion++;
        CancelShout();
        _elapsedTime = 0;
        _isShoutRunning = false;
        _cts = new CancellationTokenSource();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _elapsedTime += Time.deltaTime * _timeScale;

        if(_elapsedTime >= _shoutStartTime && !_isShoutRunning)
        {
            CancellationTokenSource cts = _cts;
            if (cts == null)
                return;

            _isShoutRunning = true;
            Shout(cts.Token, _shoutVersion).Forget();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _shoutVersion++;
        _isShoutRunning = false;
        CancelShout();
        DisableRadialBlur();
    }

    [Header("シャウト開始時間")]
    [SerializeField] private float _shoutStartTime = 0;

    [Header("シャウト終了時間")]
    [SerializeField] private float _shoutEndTime = 0;

    [Tooltip("ぼかしの強度。値が大きいほど、より多くのシステムリソースを必要とします。")]
    public ClampedIntParameter _strength = new ClampedIntParameter(1, 1, 500);

    [Tooltip("値が大きすぎると、アーチファクトが生じる可能性があります。")]
    public ClampedIntParameter _stepSize = new ClampedIntParameter(5, 1, 20);

    private Volume _shoutVolume = null;
    private bool _isShoutRunning = false;
    private float _elapsedTime = 0f;
    private CancellationTokenSource _cts = null;
    private int _shoutVersion;

    private ClampedIntParameter _saveStrength = new ClampedIntParameter(1, 1, 500);
    private ClampedIntParameter _saveStepSize = new ClampedIntParameter(5, 1, 20);

    private async UniTaskVoid Shout(CancellationToken cancellationToken, int shoutVersion)
    {
        try
        {
            if (TryGetRadialBlur(out RadialBlurSettings radialBlurSettings))
            {
                Debug.Log("取得成功");
                radialBlurSettings.active = true;
                radialBlurSettings.stepSize.value = _stepSize.value;
                radialBlurSettings.stepSize.overrideState = true;
                radialBlurSettings.strength.value = _strength.value;
                radialBlurSettings.strength.overrideState = true;
            }

            float releaseEndTime = _shoutStartTime + _shoutEndTime;
            while (_elapsedTime < releaseEndTime)
            {
                float progress = Mathf.InverseLerp(
                    _shoutStartTime,
                    releaseEndTime,
                    _elapsedTime);
                FadeOutRadialBlur(Mathf.Clamp01(progress));

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {

        }
        finally
        {
            if (shoutVersion == _shoutVersion)
                DisableRadialBlur();
        }
    }

    private void CancelShout()
    {
        if (_cts == null)
            return;

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private bool TryGetRadialBlur(out RadialBlurSettings radialBlurSettings)
    {
        radialBlurSettings = null;

        if (_shoutVolume == null)
        {
            Debug.LogError("[ShoutSMB] Shout用Volumeが未設定です。");
            return false;
        }

        if (_shoutVolume.profile == null)
        {
            Debug.LogError("[ShoutSMB] Shout用VolumeにProfileが設定されていません。");
            return false;
        }

        return _shoutVolume.profile.TryGet(out radialBlurSettings);
    }

    private void FadeOutRadialBlur(float progress)
    {
        if (!TryGetRadialBlur(out RadialBlurSettings radialBlurSettings))
            return;

        radialBlurSettings.stepSize.value = Mathf.RoundToInt(Mathf.Lerp(
            _stepSize.value,
            _saveStepSize.value,
            progress));
        radialBlurSettings.stepSize.overrideState = true;
        radialBlurSettings.strength.value = Mathf.RoundToInt(Mathf.Lerp(
            _strength.value,
            1f,
            progress));
        radialBlurSettings.strength.overrideState = true;
    }

    private void DisableRadialBlur()
    {
        if (!TryGetRadialBlur(out RadialBlurSettings radialBlurSettings))
            return;

        radialBlurSettings.stepSize.value = _saveStepSize.value;
        radialBlurSettings.stepSize.overrideState = true;
        radialBlurSettings.strength.value = _saveStrength.value;
        radialBlurSettings.strength.overrideState = true;
        radialBlurSettings.active = false;
    }
}
