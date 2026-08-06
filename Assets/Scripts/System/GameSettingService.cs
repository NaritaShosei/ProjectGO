using System;
using UnityEngine;

/// <summary>
/// ゲーム全体の設定を保持するサービス。
/// 永続化はSaveLoadServiceへ委譲する。
/// </summary>
public sealed class GameSettingService
{
    public event Action<GameSetting> OnSettingsChanged;

    public GameSetting CurrentSettings => _currentSettings.Clone();

    public GameSettingService()
    {
        _currentSettings = SaveLoadService.Load<GameSetting>();
        ApplyRuntimeSettings();
    }

    public void Save(GameSetting settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        _currentSettings = settings.Clone();
        SaveLoadService.Save(_currentSettings);
        ApplyRuntimeSettings();
        OnSettingsChanged?.Invoke(CurrentSettings);
    }

    public void ResetToDefault()
    {
        _currentSettings = SaveLoadService.Reset<GameSetting>();
        ApplyRuntimeSettings();
        OnSettingsChanged?.Invoke(CurrentSettings);
    }

    private GameSetting _currentSettings;

    private void ApplyRuntimeSettings()
    {
        if (ServiceLocator.TryGet(out SoundManager soundManager))
        {
            soundManager.ApplySettings(_currentSettings);
        }

        if (!_currentSettings.IsControllerVibrations)
        {
            ControllerVibration.Stop();
        }
    }
}

/// <summary>
/// シーン読み込み前に設定サービスを一度だけ生成する。
/// </summary>
public static class GameSettingServiceBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (!ServiceLocator.IsRegistered<GameSettingService>())
        {
            ServiceLocator.Register(new GameSettingService());
        }
    }
}
