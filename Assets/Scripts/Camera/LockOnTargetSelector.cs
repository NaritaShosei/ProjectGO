using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ロックオン対象の検索・選択ロジックを担当するクラス。
/// 候補の取得はEnemyManagerに委譲する。
/// 選定基準は「カメラ前方ベクトルと、カメラ位置から対象中心へ向かうベクトルのなす角」が
/// 最小のもの。画面内外は問わず、遮蔽・距離は選定に使わない（距離は候補取得の足切りのみ）。
/// </summary>
public class LockOnTargetSelector
{
    #region コンストラクタ

    /// <param name="playerTransform">距離計算（候補取得の足切り）に使用する</param>
    /// <param name="lockOnRange">ロックオン可能な最大距離</param>
    /// <param name="enemyManager">候補一覧の提供元</param>
    /// <param name="camera">選定スコア（前方角度）と左右判定に使用するカメラ</param>
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

    /// <summary>選定スコアと画面座標の計算に使用するカメラを更新します。</summary>
    public void SetMainCamera(Camera camera)
    {
        _camera = camera;
    }

    /// <summary>
    /// 手動ロックオン時の初回ターゲット選択。
    /// カメラ前方に最も近い（なす角が最小の）候補を返す。候補がいなければnull。
    /// </summary>
    public ILockOnTarget SelectInitialTarget()
    {
        return SelectNearestToCameraForward(GetValidCandidates());
    }

    /// <summary>
    /// 切り替え入力によるターゲット切り替え。
    /// カメラ前方に映っている（screenPos.z > 0）候補のうち、画面X座標が現在対象より
    /// 入力方向側にあるものから、カメラ前方に最も近いものを返す。
    /// </summary>
    /// <param name="currentTarget">現在のロックオン対象</param>
    /// <param name="inputDirection">正で右、負で左</param>
    public ILockOnTarget SelectSwitchTarget(ILockOnTarget currentTarget, float inputDirection)
    {
        if (currentTarget == null || _camera == null) return null;

        var candidates = GetValidCandidates(excludeTarget: currentTarget);
        if (candidates.Count == 0) return null;

        float currentScreenX = _camera.WorldToScreenPoint(
            currentTarget.GetTargetCenter().position).x;

        ILockOnTarget best = null;
        float bestAngle = float.MaxValue;

        foreach (var candidate in candidates)
        {
            Vector3 center = candidate.GetTargetCenter().position;
            Vector3 screenPos = _camera.WorldToScreenPoint(center);
            if (screenPos.z <= 0f) continue;

            float diff = screenPos.x - currentScreenX;
            if (inputDirection > 0f && diff <= 0f) continue;
            if (inputDirection < 0f && diff >= 0f) continue;

            float angle = AngleFromCameraForward(center);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// ロックオン中に現在のターゲットが撃破・削除された後の次ターゲット選択。
    /// 基準は初回選択と同じ（カメラ前方に最も近い候補）。
    /// </summary>
    public ILockOnTarget SelectNextTarget(ILockOnTarget defeatedTarget)
    {
        return SelectNearestToCameraForward(GetValidCandidates(excludeTarget: defeatedTarget));
    }

    #endregion

    #region プライベートフィールド

    private readonly Transform _playerTransform;
    private readonly float _lockOnRange;
    private readonly EnemyManager _enemyManager;
    private Camera _camera;

    #endregion

    #region プライベートメソッド

    /// <summary>
    /// ロックオン可能なターゲットのリストを取得します。
    /// プレイヤーから <see cref="_lockOnRange"/> 以内・ロックオン可能・中心Transformありのもの。
    /// </summary>
    private List<ILockOnTarget> GetValidCandidates(ILockOnTarget excludeTarget = null)
    {
        IReadOnlyList<ILockOnTarget> inRange = _enemyManager.GetLockOnTarget(
            _playerTransform.position,
            _lockOnRange);

        var result = new List<ILockOnTarget>();

        foreach (var target in inRange)
        {
            if (target == excludeTarget) continue;
            if (!target.IsLockable) continue;
            if (target.GetTargetCenter() == null) continue;

            result.Add(target);
        }

        return result;
    }

    /// <summary>
    /// 候補の中から、カメラ前方ベクトルとのなす角が最小のものを返す。
    /// 同角度のときはリスト順（先勝ち）。候補がいなければnull。
    /// </summary>
    private ILockOnTarget SelectNearestToCameraForward(List<ILockOnTarget> candidates)
    {
        if (_camera == null || candidates.Count == 0) return null;

        ILockOnTarget best = null;
        float bestAngle = float.MaxValue;

        foreach (var candidate in candidates)
        {
            float angle = AngleFromCameraForward(candidate.GetTargetCenter().position);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>カメラ前方ベクトルと「カメラ位置→worldPoint」ベクトルのなす角（度）。</summary>
    private float AngleFromCameraForward(Vector3 worldPoint)
    {
        Vector3 toTarget = worldPoint - _camera.transform.position;
        return Vector3.Angle(_camera.transform.forward, toTarget);
    }

    #endregion
}
