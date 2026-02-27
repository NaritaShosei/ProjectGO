using System;
using UnityEngine;

public interface IEnemy : ICharacter
{
    public EnemyConditionController ConditionController { get; }

    public Vector3 Position { get; }

    /// <summary>
    /// 死亡時に発火するイベント
    /// </summary>
    public event Action<IEnemy> OnDead;
    /// <summary>
    /// ノックバックの力を与える
    /// </summary>
    public void AddKnockBackForce(Vector3 direction);

    /// <summary>
    /// 攻撃の内容を渡して、内部でダメージ計算をする
    /// </summary>
    public void TakeDamage(DamageContext context);

    /// <summary>
    /// Playerの参照をもらう
    /// </summary>
    public void Init(IPlayer player);

    /// <summary>
    /// ConditionによりActionを阻害する
    /// </summary>
    public void OnConditionInterrupt();

    public void SetPosition(Vector3 position); 
}
