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
        _currentGameSettings = GameSetting.GetDefault();
        Debug.Log("[OptionModel] 初期化完了");
    }

    /// <summary>
    /// 設定の適用
    /// </summary>
    /// <param name="settings">変更済みの設定</param>
    public void Apply(GameSetting settings)
    {
        _currentGameSettings = settings.Clone();
        Debug.Log("[OptionModel] 設定を適用しました");
    }

    private GameSetting _currentGameSettings;

    private void Awake()
    {
        // Awakeで初期化
        Initialize();
    }
}