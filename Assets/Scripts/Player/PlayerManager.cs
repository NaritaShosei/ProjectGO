using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    [SerializeField] private PlayerMovement _move;
    [SerializeField] private PlayerAttack _attack;
    [SerializeField] private InputHandler _input;
    [SerializeField] private MoveData _moveData;
    private PlayerStateManager _playerStateManager;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        _playerStateManager = new PlayerStateManager();
        _move.Init(_playerStateManager, _input, ServiceLocator.Get<CameraManager>(), _moveData);
        _attack.Init(_playerStateManager, _input);

        // 回避終了時のイベントに回避攻撃に派生するメソッドを登録
        _move.OnEndDodge += _attack.FinishDodge;
    }
}
