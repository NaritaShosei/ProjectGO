using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Playerのコンポーネント")]
    [SerializeField] private PlayerMove _move;
    [SerializeField] private PlayerAttacker _attacker;
    [SerializeField] private InputHandler _input;
    [Header("データ")]
    [SerializeField] private CharacterData _data;
    public Camera MainCamera { get; private set; }
    public string PlayerName => _data.Name;
    public float PlayerMoveSpeed => _data.MoveSpeed;

    public PlayerState CurrentState { get; private set; }

    // 状態の遷移条件
    public bool CanAttack => CurrentState == PlayerState.None;
    public bool CanStartCharge => CurrentState == PlayerState.None;
    public bool IsCharging => CurrentState == PlayerState.Charge;
    public bool CanMove => CurrentState switch
    {
        PlayerState.Dodge => false,
        PlayerState.Attack => false,
        _ => true
    };
    public bool CanDodge => CurrentState switch
    {
        PlayerState.Dodge => false,
        PlayerState.Attack => false,
        _ => true
    };

    private void Awake()
    {
        _move.Init(this, _input);
        _attacker.Init(this, _input);
        MainCamera = Camera.main;
    }

    private void ChangeState(PlayerState state)
    {
        if (state == CurrentState)
        {
            Debug.Log("ステートに変化がありません"); return;
        }

        CurrentState = state;
    }

    #region 状態遷移

    public void StartAttack()
    {
        ChangeState(PlayerState.Attack);
    }

    public void StartCharge()
    {
        ChangeState(PlayerState.Charge);
    }

    public void StartDodge()
    {
        ChangeState(PlayerState.Dodge);
    }

    public void EndAction()
    {
        ChangeState(PlayerState.None);
    }
    #endregion
}

public enum PlayerState
{
    None,
    Attack,
    Charge,
    Dodge,
}