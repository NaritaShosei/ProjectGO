using UnityEngine;

public interface ISeparationService
{
    /// <summary>
    /// 集団から離れる方向を計算させる
    /// </summary>
    /// <param name="self"></param>
    /// <param name="position"></param>
    /// <param name="radius"></param>
    /// <param name="strength"></param>
    /// <returns></returns>
    Vector3 Calculate(
        IEnemy self,
        Vector3 position,
        float radius,
        float strength
    );
}
