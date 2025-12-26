using UnityEngine;

[CreateAssetMenu(fileName = "MoveData", menuName = "GameData/MoveData")]
public class MoveData : ScriptableObject
{
    public float MoveSpeed => _moveSpeed;
    public float RotateSpeed => _rotateSpeed;

    [SerializeField] private float _moveSpeed = 10;
    [SerializeField] private float _rotateSpeed = 5;
}
