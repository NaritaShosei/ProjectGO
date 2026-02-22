using UnityEngine;

public interface IEnemyAttackerSlot
{

    /// <summary>
    /// 自分をAttackerに登録できるか
    /// </summary>
    /// <param name="enemy"></param>
    /// <returns></returns>
    public bool TryAcquire(IEnemy enemy);

    /// <summary>
    /// Attacker登録解除
    /// </summary>
    /// <param name="enemy"></param>
    public void Release(IEnemy enemy);

    /// <summary>
    /// 自分がAttackerか
    /// </summary>
    /// <param name="enemy"></param>
    /// <returns></returns>
    public bool IsAttacker(IEnemy enemy);

}
