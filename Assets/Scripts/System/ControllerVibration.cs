using UnityEngine.InputSystem;

/// <summary>
/// ゲーム設定を考慮してコントローラー振動を制御する窓口。
/// </summary>
public static class ControllerVibration
{
    private static Gamepad _activeGamepad;

    public static void Play(float lowFrequency, float highFrequency)
    {
        var gamepad = Gamepad.current;
        if (!IsEnabled() || gamepad == null)
        {
            return;
        }

        Stop();
        _activeGamepad = gamepad;
        _activeGamepad.SetMotorSpeeds(lowFrequency, highFrequency);
    }

    public static void Stop()
    {
        if (_activeGamepad == null)
        {
            return;
        }

        _activeGamepad.ResetHaptics();
        _activeGamepad = null;
    }

    private static bool IsEnabled()
    {
        // 設定サービス生成前や単体テストシーンでは振動を許可する。
        return !ServiceLocator.TryGet(out GameSettingService settingsService)
            || settingsService.CurrentSettings.IsControllerVibrations;
    }
}
