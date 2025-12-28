using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    [SerializeField] private PlayerMovement _move;
    [SerializeField] private PlayerAttack _attack;
    [SerializeField] private InputHandler _input;
    [SerializeField] private AttackExecutor _attackExecutor;
    [SerializeField] private MoveData _moveData;
    private PlayerStateManager _playerStateManager;

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
        _move.Init(_playerStateManager, _input, ServiceLocator.Get<CameraManager>(), _moveData);
        // キャラクターデータを作成していないため、仮の数値を注入
        _attackExecutor.Init(100);
        _attack.Init(_playerStateManager, _input, _attackExecutor);

        // 回避終了時のイベントに回避攻撃に派生するメソッドを登録
        _move.OnEndDodge += _attack.FinishDodge;
    }
}
