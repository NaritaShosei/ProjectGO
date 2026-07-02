using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ロックオン対象の検索・選択ロジックを担当するクラス。
/// 候補の取得はEnemyManagerに委譲する。
/// </summary>
public class LockOnTargetSelector
{
    #region フィールド

    private readonly Camera _camera;
    private readonly Transform _playerTransform;
    private readonly float _lockOnRange;
    private readonly EnemyManager _enemyManager;

    #endregion

    #region コンストラクタ

    /// <param name="playerTransform">距離計算、プレイヤーの正面角度との距離を比較する</param>
    /// <param name="lockOnRange">ロックオン可能な最大距離</param>
    /// <param name="enemyManager">候補一覧の提供元</param>
    public LockOnTargetSelector(
        Transform playerTransform,
        float lockOnRange,
        EnemyManager enemyManager,
        Camera camera)
    {
        _playerTransform = playerTransform;
        _lockOnRange = lockOnRange;
        _enemyManager = enemyManager;
        _camera = camera;
    }

    #endregion

    #region パブリックメソッド

    /// <summary>
    /// 手動ロックオン時の初回ターゲット選択。
    /// 優先順位：① 画面内にいる（いない場合は無視） → ② プレイヤーキャラクターの正面に近い → ③ プレイヤーに近い
    /// 画面内の判定はEnemyのCollider.boundsを使用。Colliderがない場合はTransform.positionを点として判定。
    /// </summary>
    public ILockOnTarget SelectInitialTarget()
    {
        var candidates = GetValidCandidates();
        if (candidates.Count == 0) return null;

        ILockOnTarget screenTarget = FindNearestToCharacterCenter(candidates);
        if (screenTarget != null)
        {
            return screenTarget;
        }

        return FindNearestToPlayer(candidates);
    }

    /// <summary>
    /// 右スティック横入力によるターゲット切り替え。
    /// 入力方向側にいる画面内の敵の中で、画面中央に最も近いものを返す。
    /// </summary>
    /// <param name="currentTarget">現在のロックオン対象</param>
    /// <param name="inputDirection">正で右、負で左</param>
    public ILockOnTarget SelectSwitchTarget(ILockOnTarget currentTarget, float inputDirection)
    {
        if (currentTarget == null) return null;

        var candidates = GetValidCandidates(excludeTarget: currentTarget);
        if (candidates.Count == 0) return null;

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_camera);
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector3 currentScreenPos = _camera.WorldToScreenPoint(
            currentTarget.GetTargetCenter().position);

        ILockOnTarget best = null;
        float bestScore = float.MaxValue;

        foreach (var candidate in candidates)
        {
            // ToDo:非Componentの使用を可能にする
            if (candidate is not Component comp) continue;

            Bounds bounds = comp.GetComponent<Collider>()?.bounds
                ?? new Bounds(comp.transform.position, Vector3.one);
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds)) continue;

            Vector3 screenPos = _camera.WorldToScreenPoint(comp.transform.position);
            if (screenPos.z < 0) continue;

            // 入力方向と反対側の候補を除外
            float diff = screenPos.x - currentScreenPos.x;
            if (inputDirection > 0 && diff <= 0) continue;
            if (inputDirection < 0 && diff >= 0) continue;

            float score = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), screenCenter);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// ロックオン中に現在のターゲットを倒した後の次ターゲット選択。
    /// 優先順位は初回選択と同じ。
    /// </summary>
    public ILockOnTarget SelectNextTarget(ILockOnTarget defeatedTarget)
    {

        var candidates = GetValidCandidates(excludeTarget: defeatedTarget);
        if (candidates.Count == 0) return null;

        ILockOnTarget screenTarget = FindNearestToCharacterCenter(candidates);
        if (screenTarget != null) return screenTarget;

        return FindNearestToPlayer(candidates);
    }

    #endregion

    #region プライベートメソッド

    /// <summary>
    /// ロックオン可能なターゲットのリストを取得します。
    /// </summary>
    /// <param name="excludeTarget"></param>
    /// <returns></returns>
    private List<ILockOnTarget> GetValidCandidates(
        ILockOnTarget excludeTarget = null)
    {
        IReadOnlyList<ILockOnTarget> inRange = _enemyManager.GetLockOnTarget(
            _playerTransform.position,
            _lockOnRange);

        var result = new List<ILockOnTarget>();

        foreach (var target in inRange)
        {
            if (target == excludeTarget)
                continue;

            if (!target.IsLockable)
                continue;

            if (target.GetTargetCenter() == null)
                continue;

            result.Add(target);
        }

        return result;
    }

    /// <summary>
    /// 画面内にいる敵の中で、プレイヤーキャラクターの正面に最も近いものを返す。
    /// 画面内の判定はEnemyのCollider.boundsを使用。Colliderがない場合はTransform.positionを点として判定。
    /// 画面内に敵がいない場合はnullを返す。
    /// </summary>
    /// <param name="candidates"></param>
    /// <returns></returns>
    private ILockOnTarget FindNearestToCharacterCenter(
        List<ILockOnTarget> candidates)
    {
        ILockOnTarget best = null;
        float bestAngle = float.MaxValue;

        Plane[] frustumPlanes =
            GeometryUtility.CalculateFrustumPlanes(_camera);

        foreach (var candidate in candidates)
        {
            // ログ表示用にターゲットの名前を取得
            string targetName = candidate.GetTargetCenter() != null ? candidate.GetTargetCenter().name : "Unknown Target";

            // --- 【変更点】Componentでなくても弾かないように修正 ---
            Collider collider = null;
            if (candidate is Component comp)
            {
                // Component型である場合のみ、Colliderの取得を試みる
                collider = comp.GetComponent<Collider>();
            }
            else
            {
                Debug.Log($"[LockOn] {targetName} は純粋なデータクラス（非Component）として処理します。");
            }

            // collider が null の場合は、自動的に GetTargetCenter() の座標を基準に Bounds が作られます
            Bounds bounds = collider != null
                ? collider.bounds
                : new Bounds(
                    candidate.GetTargetCenter().position,
                    Vector3.one);

            // 画面内に映っていない敵は除外
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
            {
                Debug.Log($"[LockOn] {targetName} は画面外（視界の外）にいるため除外されました。");
                continue;
            }

            // プレイヤー正面との角度を計算
            Vector3 dirToCandidate =
                (candidate.GetTargetCenter().position - _playerTransform.position).normalized;

            float angle = Vector3.Angle(_playerTransform.forward, dirToCandidate);

            if (angle >= bestAngle)
            {
                Debug.Log($"[LockOn] {targetName} は画面内ですが、現在の最適対象（角度: {bestAngle}°）より正面ではないため保留されました。（この敵の角度: {angle}°）");
            }
            else
            {
                Debug.Log($"[LockOn] ★最優先ターゲット更新★: {targetName} (角度: {angle}°) が現在の候補に選ばれました。");
                bestAngle = angle;
                best = candidate;
            }
        }
        return best;
    }

    /// <summary>
    /// プレイヤーに最も近いエネミーを返す。
    /// </summary>
    private ILockOnTarget FindNearestToPlayer(List<ILockOnTarget> candidates)
    {
        ILockOnTarget best = null;
        float bestDist = float.MaxValue;

        foreach (var candidate in candidates)
        {
            if (candidate is not Component comp) continue;

            float dist = Vector3.Distance(_playerTransform.position, comp.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }

        return best;
    }

    #endregion
}
