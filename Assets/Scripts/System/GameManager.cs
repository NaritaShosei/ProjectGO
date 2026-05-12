using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private SequenceManager _sequenceManager;
    [SerializeField] private Player _player;
    [SerializeField] private EnemyManager _enemyManager;
    [SerializeField] private EnemyUIManager _enemyUIManager;
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private PlayerGaugeView _playerGaugeView;
    [SerializeField] private PlayerUIInitializer _inGameUIInitializer;
    [SerializeField] private ItemPickupManager _itemPickupManager;
    [SerializeField] private PlayerEffectInitializer _playerEffectInitializer;
    [SerializeField] private EXPItemManager _expManager;

    private SceneTransitionManager _sceneTransitionManager;

    private HitStopManager _hitStopManager;

    private void Awake()
    {
        // ヒットストップマネージャーを初期化してサービスロケーターに登録
        _hitStopManager = new HitStopManager();
    }

    private void Start()
    {
        InitSequence();
        InitPlayer();
        InitCameraManager();
        InitEnemyManager();
        InitUI();
        InitEffect();
        InitEXPManager();
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

        _hitStopManager?.Dispose();
    }

    private void InitPlayer()
    {
        var input = ServiceLocator.Get<InputHandler>();

        _player.Init(_skillManager, input);

        if (_player.TryGetComponent(out IModeController modeController))
        {
            _skillManager.Init(_player, modeController, _player.transform, _enemyManager);
        }

        _player.OnDead += HandleGameComplete;
    }

    private void InitCameraManager()
    {
        if (ServiceLocator.TryGet(out CameraManager cameraManager))
        {
            cameraManager.Init(_player);
        }

        if (ServiceLocator.TryGet(out LockOnManager lockOnManager))
        {
            lockOnManager.Init(_player);
        }
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

    private void InitUI()
    {
        _inGameUIInitializer.Init(_player);
        _enemyUIManager.Init(_enemyManager, _player.transform);
        _itemPickupManager.Init(_player.transform);
    }

    private void InitEffect()
    {
        _playerEffectInitializer.Init(_player, _skillManager);
    }

    private void InitEXPManager()
    {
        _expManager.Init(_player);
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
