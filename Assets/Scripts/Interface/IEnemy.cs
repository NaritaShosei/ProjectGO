using System;
using UnityEngine;

public interface IEnemy : ICharacter
{
    public event Action<IEnemy> OnDead;
    public void AddKnockBackForce(Vector3 direction);
    public void TakeDamage(AttackContext context);
}
