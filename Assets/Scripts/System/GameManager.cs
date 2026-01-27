using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private SequenceManager _sequenceManager;
    [SerializeField] private Player _player;
    [SerializeField] private EnemyManager _enemyManager;
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private CameraManager _cameraManager;

    private SceneTransitionManager _sceneTransitionManager;

    private void Start()
    {
        InitSequence();
        InitPlayer();
        InitEnemyManager();
        StartGame();
    }

    private void OnDestroy()
    {
        if (_sequenceManager != null)
        {
            _sequenceManager.OnAllSequencesComplete -= HandleGameComplete;
        }

        if (_player != null)
        {
            _player.OnDead -= HandleGameComplete;
        }
    }

    private void InitPlayer()
    {
        var input = ServiceLocator.Get<InputHandler>();

        _player.Init(_skillManager, _cameraManager, input);

        _player.OnDead += HandleGameComplete;
    }

    private void InitEnemyManager()
    {
        _enemyManager.Init(_player);
    }

    private void InitSequence()
    {
        _sequenceManager.Init(_enemyManager, _skillManager);

        _sceneTransitionManager = ServiceLocator.Get<SceneTransitionManager>();

        // SequenceManagerのイベントを購読
        _sequenceManager.OnAllSequencesComplete += HandleGameComplete;
    }

    private void StartGame()
    {
        _sequenceManager.StartSequence();
    }

    private async void HandleGameComplete()
    {
        Debug.Log("ゲーム完了。リザルトへ遷移します");

        // リザルトデータの準備などを行う場合はここで

        await _sceneTransitionManager.TransitionToResult();
    }
}