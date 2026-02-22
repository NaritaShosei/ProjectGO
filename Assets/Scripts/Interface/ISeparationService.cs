using UnityEngine;

public interface ISeparationService
{
    /// <summary>
    /// 集団から離れる方向を計算させる
    /// </summary>
    /// <param name="self">自身、クエリから除外</param>
    /// <param name="position">自分の位置</param>
    /// <param name="radius">探索半径</param>
    /// <param name="strength">分散力のスケール係数</param>
    /// <returns></returns>
    Vector3 Calculate(
        IEnemy self,
        Vector3 position,
        float radius,
        float strength
    );
}
