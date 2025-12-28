using UnityEngine;

public interface ICharacter : IHealth
{
    public Transform GetTargetCenter();
}
