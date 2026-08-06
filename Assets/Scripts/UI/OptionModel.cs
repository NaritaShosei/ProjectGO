using UnityEngine;

public class OptionModel : MonoBehaviour
{
    public GameSetting CurrentGameSettings
    {
        get
        {
            return _currentGameSettings;
        }
    }

    /// <summary>
    /// デフォルトの値を適用する
    /// </summary>
    public void Initialize()
    {
        if (!ServiceLocator.TryGet(out _gameSettingService))
        {
            // Enter Play Mode設定や単体シーン実行時にも安全に利用できるようにする。
            _gameSettingService = new GameSettingService();
            ServiceLocator.Register(_gameSettingService);
        }

        _currentGameSettings = _gameSettingService.CurrentSettings;
        Debug.Log("[OptionModel] 初期化完了");
    }

    /// <summary>
    /// 設定の適用
    /// </summary>
    /// <param name="settings">変更済みの設定</param>
    public void Apply(GameSetting settings)
    {
        if (settings == null)
        {
            Debug.LogError("[OptionModel] Applyにnullが渡されました");
            return;
        }
        _gameSettingService.Save(settings);
        _currentGameSettings = _gameSettingService.CurrentSettings;
        Debug.Log("[OptionModel] 設定を適用しました");
    }

    private GameSetting _currentGameSettings;
    private GameSettingService _gameSettingService;

    private void Awake()
    {
        // Awakeで初期化
        Initialize();
    }
}
