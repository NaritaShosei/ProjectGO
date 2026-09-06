using System.Collections.Generic;
using UnityEngine;

/// <summary>ロックオン優先度スコアの重み。値が大きいほどその項の影響が強い。スコアは小さいほど優先。</summary>
public readonly struct LockOnScoreWeights
{
    /// <summary>画面中心からのズレ（カメラ前方となす角）の重み。</summary>
    public readonly float ScreenCenter;

    /// <summary>プレイヤーからの距離の重み。</summary>
    public readonly float PlayerDistance;

    /// <summary>プレイヤーより手前（カメラ側）にいる敵へのペナルティの重み。</summary>
    public readonly float CameraSide;

    /// <summary>画面中心ズレを 0..1 に正規化する基準角（度）。この角度で 1.0 になる。</summary>
    public readonly float CenterAngleReference;

    public LockOnScoreWeights(float screenCenter, float playerDistance, float cameraSide, float centerAngleReference)
    {
        ScreenCenter = screenCenter;
        PlayerDistance = playerDistance;
        CameraSide = cameraSide;
        CenterAngleReference = centerAngleReference;
    }
}

/// <summary>
/// ロックオン対象の検索・選択ロジックを担当するクラス。
/// 候補の取得はEnemyManagerに委譲する。
/// 選定は「画面中心からのズレ」「プレイヤーからの距離」「カメラ側ペナルティ」の3項を
/// 0..1 に正規化して加重合算し、合計スコアが最小の候補を選ぶ。画面内外は問わない。
/// </summary>
public class LockOnTargetSelector
{
    #region コンストラクタ

    /// <param name="playerTransform">距離計算・カメラ側判定に使用する</param>
    /// <param name="lockOnRange">ロックオン可能な最大距離（候補の足切り兼、距離スコアの正規化基準）</param>
    /// <param name="enemyManager">候補一覧の提供元</param>
    /// <param name="camera">スコア計算と左右判定に使用するカメラ</param>
    /// <param name="weights">優先度スコアの重み</param>
    public LockOnTargetSelector(
        Transform playerTransform,
        float lockOnRange,
        EnemyManager enemyManager,
        Camera camera,
        LockOnScoreWeights weights)
    {
        _playerTransform = playerTransform;
        _lockOnRange = lockOnRange;
        _enemyManager = enemyManager;
        _camera = camera;
        _weights = weights;
    }

    #endregion

    #region パブリックメソッド

    /// <summary>スコア計算と画面座標の計算に使用するカメラを更新します。</summary>
    public void SetMainCamera(Camera camera)
    {
        _camera = camera;
    }

    /// <summary>手動ロックオン時の初回ターゲット選択。スコア最小の候補を返す（いなければnull）。</summary>
    public ILockOnTarget SelectInitialTarget()
    {
        return SelectBestByScore(GetValidCandidates());
    }

    /// <summary>
    /// 切り替え入力によるターゲット切り替え。
    /// カメラ前方に映っている（screenPos.z > 0）候補のうち、画面X座標が現在対象より
    /// 入力方向側にあるものから、スコア最小の候補を返す。
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
        float bestScore = float.MaxValue;

        foreach (var candidate in candidates)
        {
            Vector3 center = candidate.GetTargetCenter().position;
            Vector3 screenPos = _camera.WorldToScreenPoint(center);
            if (screenPos.z <= 0f) continue;

            // 入力方向と反対側の候補は除外
            float diff = screenPos.x - currentScreenX;
            if (inputDirection > 0f && diff <= 0f) continue;
            if (inputDirection < 0f && diff >= 0f) continue;

            float score = Score(center);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>現在のターゲットが撃破・削除された後の次ターゲット選択。基準は初回選択と同じ。</summary>
    public ILockOnTarget SelectNextTarget(ILockOnTarget defeatedTarget)
    {
        return SelectBestByScore(GetValidCandidates(excludeTarget: defeatedTarget));
    }

    #endregion

    #region プライベートフィールド

    private readonly Transform _playerTransform;
    private readonly float _lockOnRange;
    private readonly EnemyManager _enemyManager;
    private readonly LockOnScoreWeights _weights;
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

    /// <summary>候補の中からスコア最小のものを返す。同スコアはリスト順で先勝ち。いなければnull。</summary>
    private ILockOnTarget SelectBestByScore(List<ILockOnTarget> candidates)
    {
        if (_camera == null || candidates.Count == 0) return null;

        ILockOnTarget best = null;
        float bestScore = float.MaxValue;

        foreach (var candidate in candidates)
        {
            float score = Score(candidate.GetTargetCenter().position);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>対象中心のロックオン優先度スコア（小さいほど優先）。</summary>
    private float Score(Vector3 center)
    {
        Vector3 camPos = _camera.transform.position;

        // 1. 画面中心からのズレ（カメラ前方となす角）を 0..1 へ
        float angle = Vector3.Angle(_camera.transform.forward, center - camPos);
        float screenScore = Mathf.Clamp01(angle / Mathf.Max(0.01f, _weights.CenterAngleReference));

        // 2. プレイヤーからの距離を 0..1 へ
        float dist = Vector3.Distance(_playerTransform.position, center);
        float distScore = Mathf.Clamp01(dist / Mathf.Max(0.01f, _lockOnRange));

        // 3. プレイヤーより手前（カメラ側）にいるほどペナルティ。水平面で判定する
        Vector3 toEnemy = Flat(center - _playerTransform.position);
        Vector3 camToPlayer = Flat(_playerTransform.position - camPos);
        float cameraSideScore = 0f;
        if (toEnemy.sqrMagnitude > 0.0001f && camToPlayer.sqrMagnitude > 0.0001f)
        {
            // +1: プレイヤーの奥、-1: カメラとプレイヤーの間
            float sideDot = Vector3.Dot(toEnemy.normalized, camToPlayer.normalized);
            cameraSideScore = Mathf.Clamp01(-sideDot);
        }

        return _weights.ScreenCenter * screenScore
             + _weights.PlayerDistance * distScore
             + _weights.CameraSide * cameraSideScore;
    }

    /// <summary>Y成分を落として水平面に射影する。</summary>
    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    #endregion
}
