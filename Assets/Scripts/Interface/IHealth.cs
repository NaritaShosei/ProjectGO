using UnityEngine;

public interface IHealth
{
    public void Healing(float amount);
    // TODO：ICharacterはこれを継承するように変更する。分離のため
    // public void AddDamage(float damage);
}