using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ゲーム設定を考慮してコントローラー振動を制御する窓口。
/// </summary>
public static class ControllerVibration
{
    private static Gamepad _activeGamepad;
    private static CancellationTokenSource _stopCts;
    private static float _currentStrength;
    private static bool _isContinuous;

    public static void Play(float lowFrequency, float highFrequency)
    {
        PlayContinuous(lowFrequency, highFrequency);
    }

    public static void PlayContinuous(float lowFrequency, float highFrequency)
    {
        var gamepad = Gamepad.current;
        if (!IsEnabled() || gamepad == null)
        {
            return;
        }

        Stop();
        _activeGamepad = gamepad;
        _currentStrength = GetStrength(lowFrequency, highFrequency);
        _isContinuous = true;
        _activeGamepad.SetMotorSpeeds(Mathf.Clamp01(lowFrequency), Mathf.Clamp01(highFrequency));
    }

    /// <summary>
    /// 指定時間だけ振動する。同時発生時はLowとHighの合計が強い振動を優先する。
    /// </summary>
    public static void PlayTimed(float lowFrequency, float highFrequency, float duration)
    {
        var gamepad = Gamepad.current;
        if (!IsEnabled() || gamepad == null || duration <= 0f)
        {
            return;
        }

        lowFrequency = Mathf.Clamp01(lowFrequency);
        highFrequency = Mathf.Clamp01(highFrequency);
        float strength = GetStrength(lowFrequency, highFrequency);

        if (_activeGamepad != null && !_isContinuous && strength < _currentStrength)
        {
            return;
        }

        Stop();
        _activeGamepad = gamepad;
        _currentStrength = strength;
        _isContinuous = false;
        _activeGamepad.SetMotorSpeeds(lowFrequency, highFrequency);

        _stopCts = new CancellationTokenSource();
        StopAfterAsync(duration, _stopCts.Token).Forget();
    }

    public static void Stop()
    {
        _stopCts?.Cancel();
        _stopCts?.Dispose();
        _stopCts = null;

        if (_activeGamepad == null)
        {
            _currentStrength = 0f;
            _isContinuous = false;
            return;
        }

        _activeGamepad.ResetHaptics();
        _activeGamepad = null;
        _currentStrength = 0f;
        _isContinuous = false;
    }

    private static async UniTaskVoid StopAfterAsync(float duration, CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration), true, cancellationToken: token);
            Stop();
        }
        catch (OperationCanceledException)
        {
            // 新しい振動に置き換えられた。
        }
    }

    private static float GetStrength(float lowFrequency, float highFrequency) =>
        Mathf.Clamp01(lowFrequency) + Mathf.Clamp01(highFrequency);

    private static bool IsEnabled()
    {
        // 設定サービス生成前や単体テストシーンでは振動を許可する。
        return !ServiceLocator.TryGet(out GameSettingService settingsService)
            || settingsService.CurrentSettings.IsControllerVibrations;
    }
}
