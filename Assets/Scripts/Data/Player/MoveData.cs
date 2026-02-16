using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MoveData", menuName = "GameData/MoveData")]
public class MoveData : ScriptableObject
{
    public float RotateSpeed => _rotateSpeed;
    public DodgeData StepDodge => _stepData;
    public DodgeData RollDodge => _rollData;

    [SerializeField] private float _rotateSpeed = 5;
    [SerializeField] private DodgeData _stepData;
    [SerializeField] private DodgeData _rollData;
}

[Serializable]
public struct DodgeData
{
    public float Speed;
    public float Duration;
    public float ChainWindow; // 次の回避を受け付ける時間
}
public enum DodgeType
{
    Step,
    Roll
}
