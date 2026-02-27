using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private SequenceManager _sequenceManager;
    [SerializeField] private Player _player;
    [SerializeField] private EnemyManager _enemyManager;
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private CameraManager _cameraManager;

    [SerializeField] private PlayerGaugeView _playerGaugeView;
    private PlayerGaugePresenter _playerGaugePresenter;

    private SceneTransitionManager _sceneTransitionManager;


    private void Start()
    {
        // ヒットストップマネージャーを初期化してサービスロケーターに登録
        new HitStopManager();

        InitSequence();
        InitPlayer();
        InitCameraManager();
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

        if (_playerGaugePresenter != null)
        {
            _playerGaugePresenter.Dispose();
        }
    }

    private void InitPlayer()
    {
        var input = ServiceLocator.Get<InputHandler>();

        _player.Init(_skillManager, _cameraManager, input);

        _player.OnDead += HandleGameComplete;

        _playerGaugePresenter = new PlayerGaugePresenter(health: _player, stamina: _player, _playerGaugeView);
    }

    private void InitCameraManager()
    {
        _cameraManager.Init(_player);
    }

    private void InitEnemyManager()
    {
        _enemyManager.Init(_player);
    }

    private void InitSequence()
    {
        var input = ServiceLocator.Get<InputHandler>();

        _sequenceManager.Init(_enemyManager, _skillManager, input, _player);

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
