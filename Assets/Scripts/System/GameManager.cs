using Cysharp.Threading.Tasks;
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

    private HitStopManager _hitStopManager;

    private void Awake()
    {
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

        StartGameAfterTransitionAsync().Forget();
    }

    private void OnDestroy()
    {
        _hitStopManager?.Dispose();
    }

    private bool CheckReference(Object reference, string fieldName)
    {
        if (reference != null) return true;

        Debug.LogError($"[GameManager] Missing reference: {fieldName}", this);
        return false;
    }

    private void InitPlayer()
    {
        if (!CheckReference(_player, nameof(_player))) return;
        if (!CheckReference(_skillManager, nameof(_skillManager))) return;

        if (!ServiceLocator.TryGet(out InputHandler input))
        {
            Debug.LogError("[GameManager] InputHandler is missing.", this);
            return;
        }

        _player.Init(_skillManager, input);

        if (_player.TryGetComponent(out IModeController modeController))
        {
            _skillManager.Init(_player, modeController, _player.transform, _enemyManager);
        }
        else
        {
            Debug.LogError("[GameManager] IModeController is missing on Player.", this);
        }
    }

    private void InitCameraManager()
    {
        if (!CheckReference(_player, nameof(_player))) return;

        if (ServiceLocator.TryGet(out CameraManager cameraManager))
            cameraManager.Init(_player);
        else
            Debug.LogError("[GameManager] CameraManager is missing.", this);
    }

    private void InitEnemyManager()
    {
        if (!CheckReference(_enemyManager, nameof(_enemyManager))) return;
        if (!CheckReference(_player, nameof(_player))) return;

        _enemyManager.Init(_player);
    }

    private void InitSequence()
    {
        if (!CheckReference(_sequenceManager, nameof(_sequenceManager))) return;
        if (!CheckReference(_enemyManager, nameof(_enemyManager))) return;
        if (!CheckReference(_skillManager, nameof(_skillManager))) return;
        if (!CheckReference(_player, nameof(_player))) return;

        if (!ServiceLocator.TryGet(out InputHandler input))
        {
            Debug.LogError("[GameManager] InputHandler is missing.", this);
            return;
        }

        _sequenceManager.Init(_enemyManager, _skillManager, input, _player);
    }

    private void InitUI()
    {
        if (_inGameUIInitializer != null && _player != null)
            _inGameUIInitializer.Init(_player);
        else
            Debug.LogError("[GameManager] PlayerUIInitializer or Player is missing.", this);

        if (_enemyUIManager != null && _enemyManager != null && _player != null)
            _enemyUIManager.Init(_enemyManager, _player.transform);
        else
            Debug.LogError("[GameManager] EnemyUIManager, EnemyManager, or Player is missing.", this);

        if (_itemPickupManager != null && _player != null)
            _itemPickupManager.Init(_player.transform);
        else
            Debug.LogError("[GameManager] ItemPickupManager or Player is missing.", this);
    }

    private void InitEffect()
    {
        if (_playerEffectInitializer != null && _player != null)
            _playerEffectInitializer.Init(_player, _skillManager);
        else
            Debug.LogError("[GameManager] PlayerEffectInitializer or Player is missing.", this);
    }

    private void InitEXPManager()
    {
        if (_expManager != null)
            _expManager.Init(_player);
        else
            Debug.LogError("[GameManager] EXPItemManager is missing.", this);
    }

    private void StartGame()
    {
        if (_sequenceManager != null)
            _sequenceManager.StartSequence();
        else
            Debug.LogError("[GameManager] SequenceManager is missing.", this);
    }

    /// <summary>
    /// ロード画面のフェードアウトが完了してからゲームを開始する。
    /// インゲームシーンを直接再生した場合は、遷移中ではないため即座に開始する。
    /// </summary>
    private async UniTask StartGameAfterTransitionAsync()
    {
        if (!ServiceLocator.TryGet(out SceneTransitionManager transitionManager))
        {
            Debug.LogError(
                "[GameManager] SceneTransitionManager is missing. " +
                "ゲームの開始を中止します。",
                this);
            return;
        }

        var cancellationToken = this.GetCancellationTokenOnDestroy();

        try
        {
            await UniTask.WaitUntil(
                () => transitionManager == null || !transitionManager.IsTransitioning,
                cancellationToken: cancellationToken);
        }
        catch (System.OperationCanceledException)
        {
            return;
        }

        if (transitionManager == null || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        StartGame();
    }
}
