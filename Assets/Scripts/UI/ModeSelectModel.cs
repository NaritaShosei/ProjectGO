using UnityEngine;

public class ModeSelectModel : MonoBehaviour
{
    public string InGameModeSceneName => _inGameModeSceneName;
    public string BossModeSceneName => _bossModeSceneName;
    public string PracticeModeSceneName => _practiceModeSceneName;

    [Header("それぞれのボタンのシーン遷移先")]
    [SerializeField]
    private string _inGameModeSceneName = "InGameScene";
    [SerializeField]
    private string _bossModeSceneName = "BossModeScene";
    [SerializeField]
    private string _practiceModeSceneName = "PracticeModeScene";
}
