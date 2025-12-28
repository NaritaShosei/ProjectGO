using UnityEngine;

public class PlayerManager : MonoBehaviour, IPlayer, IStamina
{
    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public void Healing(float amount)
    {
        if (_playerStateManager.IsDead()) { return; }

        _playerStats.Heal(amount);
    }

    public void TakeDamage(float damage)
    {
        if (_playerStateManager.IsDead()) { return; }
        // TODO:被弾ダメージ計算を考慮する
        _playerStats.TakeDamage(damage);
    }
    public bool TryUseStamina(float amount)
    {
        return _playerStats.UseStamina(amount);
    }

    public float GetDodgeStaminaCost()
    {
        return _playerData.DodgeStaminaCost;
    }

    [SerializeField] private PlayerMovement _move;
    [SerializeField] private PlayerAttack _attack;
    [SerializeField] private InputHandler _input;
    [SerializeField] private AttackExecutor _attackExecutor;
    [SerializeField] private MoveData _moveData;
    [SerializeField] private PlayerData _playerData;
    [SerializeField] private Transform _targetCenter;
    private PlayerStateManager _playerStateManager;
    private PlayerStats _playerStats;

    private void Awake()
    {
        Init();
    }

    private void Update()
    {
        RegenerateStamina();
    }

    private void OnDestroy()
    {
        if (_playerStats != null)
        {
            _playerStats.OnDead -= OnPlayerDead;
        }

        if (_move != null)
        {
            _move.OnEndDodge -= _attack.FinishDodge;
        }
    }

    private void Init()
    {
        _playerStateManager = new PlayerStateManager();
        _playerStats = new PlayerStats(_playerData.Stats);

        _playerStats.OnDead += OnPlayerDead;

        _move?.Init(
            _playerStateManager,
            _input,
            ServiceLocator.Get<CameraManager>(),
            _moveData,
            this);

        _attackExecutor?.Init(_playerData.AttackPower);
        _attack?.Init(_playerStateManager, _input, _attackExecutor);

        // 回避終了時のイベントに回避攻撃に派生するメソッドを登録
        if (_move != null && _attack != null)
        {
            _move.OnEndDodge += _attack.FinishDodge;
        }
    }

    private void RegenerateStamina()
    {
        _playerStats.RegenerateStamina(_playerData.StaminaRegenPerSecond);
    }

    private void OnPlayerDead()
    {
        _playerStateManager.ChangeState(PlayerState.Dead);
    }
}
