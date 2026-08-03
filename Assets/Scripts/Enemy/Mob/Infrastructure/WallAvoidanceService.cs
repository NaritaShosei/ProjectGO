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
        // 壁抜け防止はXZ平面だけに適用する。
        // Y成分まで止めると、ノックバック中の上昇・落下が壁接触で不自然に停止する。
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0f, displacement.z);
        if (_wallMask == 0 || horizontalDisplacement.sqrMagnitude <= Mathf.Epsilon)
            return displacement;

        float distance = horizontalDisplacement.magnitude;

        // Boundsをわずかに縮め、壁に接しているだけの状態を「貫通」と誤判定しないようにする。
        // 極端に小さいコライダーでもBoxCastの大きさが0にならないよう下限を設ける。
        Vector3 halfExtents = bounds.extents - Vector3.one * _skinWidth;
        halfExtents.x = Mathf.Max(halfExtents.x, _minimumExtent);
        halfExtents.y = Mathf.Max(halfExtents.y, _minimumExtent);
        halfExtents.z = Mathf.Max(halfExtents.z, _minimumExtent);

        // 敵の体積を移動方向へ飛ばし、移動区間内にWallレイヤーがあるか調べる。
        // distanceにskinWidthを加えることで、壁の直前まで来たフレームでも確実に検出する。
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
            // 経路上に壁がなければ、要求された移動量をそのまま許可する。
            return displacement;
        }

        // 壁へ密着しすぎないよう、ヒット地点よりskinWidthだけ手前で止める。
        float allowedDistance = Mathf.Max(0f, hit.distance - _skinWidth);
        float movementRatio = allowedDistance / distance;

        // 水平成分だけを短縮し、ノックバックの上下成分は元の値を維持する。
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

        // Boundsとコライダーの実座標は現在のTransformを基準に取得する。
        // positionは補正のたびに変わるため、差分offsetを加えて仮想的な候補位置を作る。
        Bounds bounds = collider.bounds;
        Vector3 colliderPosition = collider.transform.position;

        // 角や複数の壁に挟まれた場合、1回の押し戻しでは別の壁に重なることがある。
        // そのため、重なりがなくなるまで最大回数の範囲で繰り返す。
        for (int iteration = 0; iteration < _maxSpawnResolveIterations; iteration++)
        {
            Vector3 offset = position - colliderPosition;

            // 候補位置周辺のWallコライダーを列挙する。
            // NonAlloc版と共有バッファを使い、移動のたびにGCを発生させない。
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

                // 実際に形状が交差している場合だけ、最短の脱出方向と貫通距離を取得する。
                // OverlapBoxは候補を広めに拾うため、ComputePenetrationによる確定判定が必要。
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

                // 貫通距離ぴったりでは再び接触判定される可能性があるため、
                // skinWidth分だけ余裕を持って壁の外へ押し戻す。
                position += direction * (distance + _skinWidth);
                resolvedAny = true;
            }

            // どの壁とも実際には交差していなければ、安全な位置として処理を終了する。
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
