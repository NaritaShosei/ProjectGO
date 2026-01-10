using UnityEngine;

public abstract class BossAttackBase : ScriptableObject
{
    public float Interval => _interval;

    /// <summary>
    /// 攻撃実行（各攻撃で中身を書く）
    /// </summary>
    public abstract void Execute(BossAttackContext context);

    [SerializeField] protected private float _interval;
    [SerializeField] protected private float _damage;
}

public struct BossAttackContext
{
    public Transform BossTransform;
    public Transform Player;
}
