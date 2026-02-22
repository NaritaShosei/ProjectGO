using UnityEngine;

public interface IWallAvoidanceService
{
    /// <summary>
    /// 壁から離れるベクトルを計算する
    /// </summary>
    /// <param name="self"></param>
    /// <param name="forward"></param>
    /// <param name="detectDistance"></param>
    /// <param name="strength"></param>
    /// <returns></returns>
    Vector3 CalculateAvoidance(
        Vector3 self,
        Vector3 forward,
        float detectDistance,
        float strength
    );
}
