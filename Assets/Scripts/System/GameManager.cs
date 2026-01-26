using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private SequenceManager _sequenceManager;
    private SceneTransitionManager _sceneTransitionManager;

    private void Start()
    {
        if (!ServiceLocator.TryGet(out _sceneTransitionManager))
        {
            Debug.LogError("SceneTransitionManagerが登録されていません");
            enabled = false;
            return;
        }

        // SequenceManagerのイベントを購読
        _sequenceManager.OnAllSequencesComplete += HandleGameComplete;
    }

    private void OnDestroy()
    {
        if (_sequenceManager != null)
        {
            _sequenceManager.OnAllSequencesComplete -= HandleGameComplete;
        }
    }

    private async void HandleGameComplete()
    {
        Debug.Log("ゲーム完了。リザルトへ遷移します");

        // リザルトデータの準備などを行う場合はここで

        await _sceneTransitionManager.TransitionToResult();
    }
}