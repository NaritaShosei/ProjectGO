using UnityEngine.InputSystem;

/// <summary>
/// ゲーム設定を考慮してコントローラー振動を制御する窓口。
/// </summary>
public static class ControllerVibration
{
    public static void Play(float lowFrequency, float highFrequency)
    {
        if (!IsEnabled() || Gamepad.current == null)
        {
            return;
        }

        Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);
    }

    public static void Stop()
    {
        Gamepad.current?.ResetHaptics();
    }

    private static bool IsEnabled()
    {
        return !ServiceLocator.TryGet(out GameSettingService settingsService)
            || settingsService.CurrentSettings.IsControllerVibrations;
    }
}
