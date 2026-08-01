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

    /// <summary>
    /// 移動先までコライダーをキャストし、壁を越えない移動量に制限する。
    /// 壁回避ベクトルだけでは防げない高速移動やノックバックの壁抜けにも適用する。
    /// </summary>
    public Vector3 ClampMovement(Bounds bounds, Vector3 displacement)
    {
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0f, displacement.z);
        if (_wallMask == 0 || horizontalDisplacement.sqrMagnitude <= Mathf.Epsilon)
            return displacement;

        float distance = horizontalDisplacement.magnitude;
        Vector3 halfExtents = bounds.extents - Vector3.one * _skinWidth;
        halfExtents.x = Mathf.Max(halfExtents.x, _minimumExtent);
        halfExtents.y = Mathf.Max(halfExtents.y, _minimumExtent);
        halfExtents.z = Mathf.Max(halfExtents.z, _minimumExtent);

        if (!Physics.BoxCast(
                bounds.center,
                halfExtents,
                horizontalDisplacement / distance,
                out RaycastHit hit,
                Quaternion.identity,
                distance + _skinWidth,
                _wallMask,
                QueryTriggerInteraction.Ignore))
        {
            return displacement;
        }

        float allowedDistance = Mathf.Max(0f, hit.distance - _skinWidth);
        float movementRatio = allowedDistance / distance;
        return new Vector3(
            horizontalDisplacement.x * movementRatio,
            displacement.y,
            horizontalDisplacement.z * movementRatio
        );
    }

    /// <summary>
    /// スポーン位置で壁と重なっている場合、最短方向へ押し戻して貫通を解消する。
    /// </summary>
    public Vector3 ResolveSpawnPosition(Collider collider, Vector3 position)
    {
        if (_wallMask == 0 || collider == null)
            return position;

        Bounds bounds = collider.bounds;
        Vector3 colliderPosition = collider.transform.position;

        for (int iteration = 0; iteration < _maxSpawnResolveIterations; iteration++)
        {
            Vector3 offset = position - colliderPosition;
            int overlapCount = Physics.OverlapBoxNonAlloc(
                bounds.center + offset,
                bounds.extents,
                _overlapBuffer,
                Quaternion.identity,
                _wallMask,
                QueryTriggerInteraction.Ignore
            );

            bool resolvedAny = false;
            for (int i = 0; i < overlapCount; i++)
            {
                Collider wall = _overlapBuffer[i];
                if (!Physics.ComputePenetration(
                        collider,
                        colliderPosition + offset,
                        collider.transform.rotation,
                        wall,
                        wall.transform.position,
                        wall.transform.rotation,
                        out Vector3 direction,
                        out float distance))
                {
                    continue;
                }

                position += direction * (distance + _skinWidth);
                resolvedAny = true;
            }

            if (!resolvedAny)
                break;
        }

        return position;
    }

    private readonly LayerMask _wallMask;
    private readonly Collider[] _overlapBuffer = new Collider[32];
    private const float _skinWidth = 0.02f;
    private const float _minimumExtent = 0.01f;
    private const int _maxSpawnResolveIterations = 8;
}
