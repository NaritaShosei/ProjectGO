using System.Collections.Generic;
using BossEnemy.Armor;
using BossEnemy.Character;
using BossEnemy.Enum;
using BossEnemy.Interface;
using UnityEngine;

/// <summary>
/// ボスの両足の鎧の生死に応じて、右足/左足/頭をロックオン候補として LockOnTargetSelector へ供給する。
/// 対象の選定自体（画面中心近さ・プレイヤー距離などのスコア）は LockOnTargetSelector の通常ロジックに委ねる。
/// Boss側のスクリプト・プレハブは変更せず、既存の公開APIだけを参照する。
/// </summary>
public sealed class BossLegLockOnController
{
    /// <summary>LockOnTargetSelectorへ供給する候補一覧。ボス不在時は空。</summary>
    public IReadOnlyList<ILockOnTarget> Candidates => _candidates;

    /// <summary>
    /// EnemyManager由来の候補から除外すべきか判定する。
    /// ボス本体の姿勢(Posture)連動パーツ(BossCharacterPartsView)は、本クラスが供給する
    /// 右足/左足/頭の候補と役割が重複する（かつ鎧の生死と無関係にロック可否が切り替わる）ため、
    /// 常に除外して二重登録・意図しない対象への切り替わりを防ぐ。
    /// </summary>
    public bool ShouldExcludeFromDefaultPool(ILockOnTarget target) => target is BossCharacterPartsView;

    public BossLegLockOnController(EnemyManager enemyManager)
    {
        _enemyManager = enemyManager;
        _enemyManager.OnEnemySpawned += HandleEnemySpawned;
        _enemyManager.OnBossDefeated += HandleBossGone;
        _enemyManager.OnEnemyForceRemoved += HandleEnemyForceRemoved;
    }

    /// <summary>右足/左足の鎧の生死から、右足/左足/頭のロック可否を毎Tick再計算する。</summary>
    public void Tick()
    {
        if (_rightLegArmor == null || _leftLegArmor == null) return;

        bool isRightLegArmorAlive = !_rightLegArmor.IsBroken;
        bool isLeftLegArmorAlive = !_leftLegArmor.IsBroken;

        _rightLegTarget.SetLockable(isRightLegArmorAlive);
        _leftLegTarget.SetLockable(isLeftLegArmorAlive);
        _headTarget.SetLockable(!isRightLegArmorAlive && !isLeftLegArmorAlive);
    }

    private void HandleEnemySpawned(IEnemy enemy)
    {
        if (enemy is not IBossEnemyCharacterView bossView) return;

        Transform headTransform = FindHeadAnglePoint(bossView.Self);

        BossArmorView rightLegArmor = null;
        BossArmorView leftLegArmor = null;
        Transform rightLegTransform = null;
        Transform leftLegTransform = null;

        foreach (var parts in bossView.ActiveBossEnemyPartsView)
        {
            if (parts?.Armor == null) continue;

            if (parts.Armor.AttachmentPoints == ArmorAttachmentType.RightLeg)
            {
                rightLegArmor = parts.Armor;
                rightLegTransform = parts.GetTargetCenter();
            }
            else if (parts.Armor.AttachmentPoints == ArmorAttachmentType.LeftLeg)
            {
                leftLegArmor = parts.Armor;
                leftLegTransform = parts.GetTargetCenter();
            }
        }

        if (headTransform == null || rightLegArmor == null || leftLegArmor == null)
        {
            Debug.LogWarning(
                "[BossLegLockOnController] 脚/頭ロックオンに必要な参照が揃わなかったため無効化します。" +
                $"(head:{headTransform != null}, rightLeg:{rightLegArmor != null}, leftLeg:{leftLegArmor != null})",
                bossView.Self);
            Reset();
            return;
        }

        _rightLegArmor = rightLegArmor;
        _leftLegArmor = leftLegArmor;
        _rightLegTarget = new BossPartLockOnTarget(rightLegTransform);
        _leftLegTarget = new BossPartLockOnTarget(leftLegTransform);
        _headTarget = new BossPartLockOnTarget(headTransform);
        _candidates = new List<ILockOnTarget> { _rightLegTarget, _leftLegTarget, _headTarget };

        // 次のTickを待たずに現在の鎧状態を反映しておく
        Tick();
    }

    private void HandleBossGone() => Reset();

    private void HandleEnemyForceRemoved(IEnemy enemy)
    {
        if (enemy is IBossEnemyCharacterView) Reset();
    }

    private void Reset()
    {
        _rightLegArmor = null;
        _leftLegArmor = null;
        _rightLegTarget = null;
        _leftLegTarget = null;
        _headTarget = null;
        _candidates = _emptyCandidates;
    }

    /// <summary>ボス子階層から頭側のCameraAnglePoint（BossCameraControllerが使うものと同じ）を探す。</summary>
    private static Transform FindHeadAnglePoint(Transform bossRoot)
    {
        foreach (CameraAnglePoint point in bossRoot.GetComponentsInChildren<CameraAnglePoint>(true))
        {
            if (point.AnglePoint == CameraAnglePointType.Top) return point.GetTargetCenter();
        }

        return null;
    }

    private static readonly IReadOnlyList<ILockOnTarget> _emptyCandidates = new List<ILockOnTarget>();

    private readonly EnemyManager _enemyManager;
    private IReadOnlyList<ILockOnTarget> _candidates = _emptyCandidates;

    private BossArmorView _rightLegArmor;
    private BossArmorView _leftLegArmor;
    private BossPartLockOnTarget _rightLegTarget;
    private BossPartLockOnTarget _leftLegTarget;
    private BossPartLockOnTarget _headTarget;
}

/// <summary>
/// Boss本体のパーツクラスに依存せず、Camera側だけでロック可否を管理する軽量ロックオン対象。
/// </summary>
public sealed class BossPartLockOnTarget : ILockOnTarget
{
    public bool IsLockable { get; private set; }

    public Transform GetTargetCenter() => _center;

    public BossPartLockOnTarget(Transform center) => _center = center;

    public void SetLockable(bool lockable) => IsLockable = lockable;

    private readonly Transform _center;
}
