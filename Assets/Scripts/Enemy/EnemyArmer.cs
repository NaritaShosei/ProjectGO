using System;
using UnityEngine;

// ボス用のオブジェクト
public class EnemyArmer : MonoBehaviour, IEnemy
{
    public event Action<IEnemy> OnDead;

    public bool IsBroken => _hp <= 0;
    public void AddKnockBackForce(Vector3 direction)
    {
        // ノックバックなし
    }

    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public void TakeDamage(DamageContext context)
    {
        if (context.PlayerMode != PlayerMode.Warrior) { return; }

        _hp -= context.AttackPower;

        if (_hp <= 0)
        {
            OnDead?.Invoke(this);
            Break();
        }
    }
    public void Init(IPlayer player)
    {
        // プレイヤーの参照は不要
    }
    public void OnConditionInterrupt() { }


    [SerializeField] private float _hp = 50;
    [SerializeField] private GameObject _core;
    [SerializeField] private Transform _targetCenter;

    private void Break()
    {
        if (_core)
        {
            _core.SetActive(true);
        }

        gameObject.SetActive(false);
    }
}
