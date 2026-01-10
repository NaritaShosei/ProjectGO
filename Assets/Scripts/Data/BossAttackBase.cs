using UnityEngine;

[CreateAssetMenu(fileName = "BossAttackData", menuName = "GameData/BossAttackData")]
public abstract class BossAttackBase : ScriptableObject
{
    public float Interval;

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
