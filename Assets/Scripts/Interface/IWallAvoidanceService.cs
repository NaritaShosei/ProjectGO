using UnityEngine;

public interface IWallAvoidanceService
{
    /// <summary>
    /// 壁から離れるベクトルを計算する
    /// </summary>
    /// <param name="self">自身の位置</param>
    /// <param name="forward">進行方向ベクトル</param>
    /// <param name="detectDistance">壁検出距離</param>
    /// <param name="strength">回避力のスケール係数</param>
    /// <returns>壁を検出すれば反射方向に、なければVector3.zero</returns>
    Vector3 CalculateAvoidance(
        Vector3 self,
        Vector3 forward,
        float detectDistance,
        float strength
    );

    /// <summary>
    /// 移動経路上の壁を検出し、壁の手前で止まる安全な移動量を返す。
    /// </summary>
    Vector3 ClampMovement(Bounds bounds, Vector3 displacement);

    /// <summary>
    /// 指定位置で壁と重なっているコライダーを、最短方向へ押し戻した座標を返す。
    /// </summary>
    Vector3 ResolveSpawnPosition(Collider collider, Vector3 position);
}
