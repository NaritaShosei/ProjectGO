using System;
using UnityEngine;

// ボス用のオブジェクト
public class EnemyArmer : MonoBehaviour, IEnemy
{
    public event Action<IEnemy> OnDead;
    public event Action<float, float> OnHealthChanged;

    public EnemyConditionController ConditionController { get; }

    // public event Action<DamagePopupViewModel> OnDamageDealt;

    public Vector3 Position { get => transform.position; }

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

        // InvokeOnDamageDealt((int)context.AttackPower, isWeakPoint: false, context.IsCritical);

        if (_hp <= 0)
        {
            OnDead?.Invoke(this);
            Break();

            context.OnHitResult?.Invoke(
                new HitResult
                {
                    IsKill = false,
                    IsArmorBreak = true,
                    IsWeakPoint = false
                });
        }
    }

    /*
    public void InvokeOnDamageDealt(int damage, bool isWeakPoint, bool isCritical)
    {
        OnDamageDealt?.Invoke(
            new DamagePopupViewModel(
                damage: damage,
                isWeakPoint: isWeakPoint,
                isCritical: isCritical,
                worldPosition: GetTargetCenter().position
                )
            );
    }
    */

    public void Init(IPlayer player)
    {
        // プレイヤーの参照は不要
    }
    public void OnConditionInterrupt() { }
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void Healing(float amount)
    {

    }

    public void TakeDamage(float damage)
    {

    }

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
