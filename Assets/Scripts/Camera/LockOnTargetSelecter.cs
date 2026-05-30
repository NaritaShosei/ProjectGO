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

    /// <param name="camera">スクリーン座標変換に使用するカメラ</param>
    /// <param name="playerTransform">距離計算の基準となるプレイヤー</param>
    /// <param name="lockOnRange">ロックオン可能な最大距離</param>
    /// <param name="enemyManager">候補一覧の提供元</param>
    public LockOnTargetSelector(
        Camera camera,
        Transform playerTransform,
        float lockOnRange,
        EnemyManager enemyManager)
    {
        _camera = camera;
        _playerTransform = playerTransform;
        _lockOnRange = lockOnRange;
        _enemyManager = enemyManager;
    }

    #endregion

    #region パブリックメソッド

    /// <summary>
    /// 手動ロックオン時の初回ターゲット選択。
    /// 優先順位：① カメラ中心に近い画面内エネミー → ② 最寄りエネミー
    /// </summary>
    public ILockOnTarget SelectInitialTarget()
    {
        var candidates = GetValidCandidates();
        if (candidates.Count == 0) return null;

        ILockOnTarget screenTarget = FindNearestToScreenCenter(candidates);
        if (screenTarget != null) return screenTarget;

        return FindNearestToPlayer(candidates);
    }

    /// <summary>
    /// 攻撃時自動ロックオン時のターゲット選択。
    /// 最寄りエネミーを選択する。
    /// </summary>
    public ILockOnTarget SelectNearestTarget()
    {
        var candidates = GetValidCandidates();
        if (candidates.Count == 0) return null;

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

        ILockOnTarget screenTarget = FindNearestToScreenCenter(candidates);
        if (screenTarget != null) return screenTarget;

        return FindNearestToPlayer(candidates);
    }

    #endregion

    #region プライベートメソッド

    /// <summary>
    /// 有効な候補一覧を返す。
    /// EnemyManagerのGetEnemiesInRangeで距離フィルタ済みの候補を取得し、
    /// IsLockable・TargetCenterの存在を追加チェックする。
    /// </summary>
    private List<ILockOnTarget> GetValidCandidates(ILockOnTarget excludeTarget = null)
    {
        // EnemyManagerで距離フィルタ済みの候補を取得
        // IEnemy は ILockOnTarget を継承しているため直接キャストできる
        var inRange = _enemyManager.GetEnemiesInRange(_playerTransform.position, _lockOnRange);

        var result = new List<ILockOnTarget>();
        foreach (var enemy in inRange)
        {
            ILockOnTarget candidate = enemy as ILockOnTarget;
            if (candidate == null) continue;
            if (candidate == excludeTarget) continue;
            if (!candidate.IsLockable) continue;
            if (candidate.GetTargetCenter() == null) continue;

            result.Add(candidate);
        }

        return result;
    }

    /// <summary>
    /// 画面内エネミーの中で画面中央に最も近いものを返す。
    /// 画面内に誰もいない場合はnullを返す。
    /// </summary>
    private ILockOnTarget FindNearestToScreenCenter(List<ILockOnTarget> candidates)
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_camera);
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        ILockOnTarget best = null;
        float bestScore = float.MaxValue;

        foreach (var candidate in candidates)
        {
            if (candidate is not Component comp) continue;

            Bounds bounds = comp.GetComponent<Collider>()?.bounds
                ?? new Bounds(comp.transform.position, Vector3.one);
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds)) continue;

            Vector3 screenPos = _camera.WorldToScreenPoint(candidate.GetTargetCenter().position);
            if (screenPos.z < 0) continue;

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