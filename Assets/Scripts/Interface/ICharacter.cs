using UnityEngine;

public interface ICharacter 
{
    public void AddDamage(float damage);
    public Transform GetTargetCenter();
}
