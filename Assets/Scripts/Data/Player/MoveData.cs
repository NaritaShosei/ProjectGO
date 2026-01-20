using UnityEngine;

[CreateAssetMenu(fileName = "MoveData", menuName = "GameData/MoveData")]
public class MoveData : ScriptableObject
{
    public float RotateSpeed => _rotateSpeed;
    public float DodgeSpeed => _dodgeSpeed;
    public float DodgeDuration => _dodgeDuration;

    [SerializeField] private float _rotateSpeed = 5;
    [SerializeField] private float _dodgeSpeed = 10;
    [SerializeField] private float _dodgeDuration = 1;
}
