using System;
using UnityEngine;

/// <summary>
/// ゲーム全体の設定を保持するサービス。
/// 永続化はSaveLoadServiceへ委譲する。
/// </summary>
public sealed class GameSettingService
{
    public event Action<GameSetting> OnSettingsChanged;

    // 呼び出し側から保持中の設定を直接書き換えられないようコピーを返す。
    public GameSetting CurrentSettings => _currentSettings.Clone();

    public GameSettingService()
    {
        // 起動時にディスクから復元し、利用可能なシステムへ初期値を反映する。
        _currentSettings = SaveLoadService.Load<GameSetting>();
        ApplyRuntimeSettings();
    }

    public void Save(GameSetting settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        // 保持・永続化・ゲームへの反映をこのサービス経由に統一する。
        var newSettings = settings.Clone();
        SaveLoadService.Save(newSettings);
        _currentSettings = newSettings;
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
        // SoundManagerがまだ生成されていない場合は、生成時に現在値を取得して反映する。
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
    // 最初のシーンのAwakeより前に登録し、すべてのシーンから同じ設定を参照可能にする。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (!ServiceLocator.IsRegistered<GameSettingService>())
        {
            ServiceLocator.Register(new GameSettingService());
        }
    }
}
