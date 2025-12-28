using UnityEngine;

[CreateAssetMenu(fileName = "MoveData", menuName = "GameData/MoveData")]
public class MoveData : ScriptableObject
{
    public float MoveSpeed => _moveSpeed;
    public float RotateSpeed => _rotateSpeed;
    public float DodgeSpeed => _dodgeSpeed;
    public float DodgeDuration => _dodgeDuration;
    public float DodgeStaminaCost => _dodgeStaminaCost;

    [SerializeField] private float _moveSpeed = 10;
    [SerializeField] private float _rotateSpeed = 5;
    [SerializeField] private float _dodgeSpeed = 10;
    [SerializeField] private float _dodgeDuration = 1;
    [SerializeField] private float _dodgeStaminaCost = 10;
}
