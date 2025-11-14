using UnityEngine;

public interface IEnemy : ICharacter
{
    public void AddKnockBackForce(Vector3 direction);
}
