using UnityEngine;

/// <summary>
/// Raycastで前方の壁を検知し、壁面に沿う方向への回避ベクトルを返すサービス
/// 反射ベクトルを使用することで壁に平行な方向へ自然に誘導する
/// </summary>
public sealed class WallAvoidanceService : IWallAvoidanceService
{
    /// <summary>
    /// 壁判定に使用するLayerMaskはコンストラクタで注入する
    /// </summary>
    public WallAvoidanceService(LayerMask wallMask)
    {
        _wallMask = wallMask;
    }

    public Vector3 CalculateAvoidance(
        Vector3 self,
        Vector3 forward,
        float detectDistance,
        float strength
    )
    {
        // LayerMaskが未設定（0）の場合は回避しない
        if (_wallMask == 0) return Vector3.zero;

        if (Physics.Raycast(self, forward, out var hit, detectDistance, _wallMask))
        {
            // 壁面法線に対する反射ベクトルを計算する
            // 壁への垂直成分を反転することで、壁に平行な方向へ誘導する
            Vector3 reflect = Vector3.Reflect(forward, hit.normal);
            return reflect.normalized * strength;
        }

        return Vector3.zero;
    }

    private readonly LayerMask _wallMask;
}
