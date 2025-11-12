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

    private void Awake()
    {
        _move.Init(this, _input);
        _attacker.Init(this, _input);
        MainCamera = Camera.main;
    }
}
