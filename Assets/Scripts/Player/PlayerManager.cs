using UnityEngine;

public class PlayerManager : MonoBehaviour, IPlayer
{
    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public void Healing(float amount)
    {
        _playerStats.Heal(amount);
    }

    public void TakeDamage(float damage)
    {
        _playerStats.TakeDamage(damage);
    }

    [SerializeField] private PlayerMovement _move;
    [SerializeField] private PlayerAttack _attack;
    [SerializeField] private InputHandler _input;
    [SerializeField] private AttackExecutor _attackExecutor;
    [SerializeField] private MoveData _moveData;
    [SerializeField] private StatsData _statData;
    [SerializeField] private Transform _targetCenter;
    private PlayerStateManager _playerStateManager;
    private PlayerStats _playerStats;

    private void Awake()
    {
        Init();
    }

    private void OnDestroy()
    {
        if (_move != null)
        {
            _move.OnEndDodge -= _attack.FinishDodge;
        }
    }

    private void Init()
    {
        _playerStateManager = new PlayerStateManager();
        _playerStats = new PlayerStats(_statData, _playerStateManager);

        _move?.Init(_playerStateManager, _input, ServiceLocator.Get<CameraManager>(), _moveData);
        // キャラクターデータを作成していないため、仮の数値を注入
        _attackExecutor?.Init(100);
        _attack?.Init(_playerStateManager, _input, _attackExecutor);

        // 回避終了時のイベントに回避攻撃に派生するメソッドを登録
        if (_move != null && _attack != null)
        {
            _move.OnEndDodge += _attack.FinishDodge;
        }
    }
}
