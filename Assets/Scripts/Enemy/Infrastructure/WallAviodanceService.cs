using UnityEngine;

public class WallAvoidanceService : IWallAvoidanceService
{

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="wallMask"></param>
    public WallAvoidanceService(LayerMask wallMask)
    {
        this.wallMask = wallMask;
    }

    public Vector3 CalculateAvoidance(
        Vector3 self,
        Vector3 forward,
        float detectDistance,
        float strength
    )
    {
        if (Physics.Raycast(self, forward, out var hit, detectDistance, wallMask))
        {
            // 反射ベクトル計算
            // つまり壁に対する平行ベクトルを維持し、垂直ベクトルだけ反対方向にしている
            Vector3 reflect = Vector3.Reflect(forward, hit.normal);
            return reflect.normalized * strength;
        }
        return Vector3.zero;
    }

    private readonly LayerMask wallMask;

}
