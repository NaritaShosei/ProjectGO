using UnityEngine;

/// <summary>
/// ストーンリングのクラス。
/// 硬直とシールド破壊時の死亡処理を持つ
/// </summary>
public sealed class StoneRingEnemy : MobEnemy,IEnemyGroupMember
{
    public EnemyGroup Group => _group;
    public bool IsGroupLeader => _isGroupLeader;

    public void AssignGroup(
        EnemyGroup group,
        bool isLeader)
    {
        _group = group;
        _isGroupLeader = isLeader;
    }

    public void ClearGroup()
    {
        _group = null;
        _isGroupLeader = false;
    }

    public void SetGroupLeader(bool isLeader)
    {
        _isGroupLeader = isLeader;
    }

    /// <summary>
    /// ストーンリングの初期化
    /// </summary>
    public override void Init()
    {
        if (IsInitialized) return;
        base.Init();

        OnArmorBroken += HandleArmorBroken;

        if (_attack != null)
        {
            _attack.OnAttackFinished += HandleAttackFinished;
        }
        else 
        {
            Debug.LogWarning($"{nameof(StoneRingEnemy)}: Attackが未設定です。攻撃後の硬直は発生しません。");
        }
    }

    /// <summary>
    /// ビヘイビアの登録
    /// </summary>
    /// <param name="initCtx"></param>
    protected override void RegisterBehaviours(
        BehaviourInitContext initCtx)
    {
        base.RegisterBehaviours(initCtx);

        _groupFollowBehaviour =
            new GroupFollowBehaviour(
                this,
                _services,
                _groupFollowDistance,
                _groupFormationHalfWidth,
                _groupRearDistance,
                _groupFollowStopDistance);

        _groupFollowBehaviour.Init(initCtx);
        _runner.Register(_groupFollowBehaviour);

        _postAttackStun =
            new PostAttackStunBehaviour(
                _postAttackRecoveryDuration);

        _postAttackStun.Init(initCtx);

        if (_services.AttackerSlot is
            IEnemyFormationSystem formationSystem)
        {
            _groupPromotion =
                new GroupPromotionBehaviour(
                    this,
                    formationSystem);

            _runner.Register(
                _groupPromotion);

            _groupPromotion.Init(
                initCtx);
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(StoneRingEnemy)}: " +
                "FormationSystemを取得できません。");
        }
    }

    /// <summary>
    /// 攻撃終了後に硬直を開始する。
    /// </summary>
    private void HandleAttackFinished()
    {
        if (_isDead || _postAttackStun == null) return;
        Debug.Log($"{nameof(StoneRingEnemy)}: 攻撃終了後の硬直を開始します。");
        _runner.ForceBehaviour(_postAttackStun);
    }

    /// <summary>
    /// シールド破壊時に死亡する。
    /// </summary>
    private void HandleArmorBroken(IEnemy enemy)
    {
        if (_isDead) return;

        _stats.Kill();
    }

    protected override void OnDestroy()
    {
        OnArmorBroken -= HandleArmorBroken;

        if (_attack != null)
        {
            _attack.OnAttackFinished -= HandleAttackFinished;
        }

        base.OnDestroy();
    }

    [Header("Attack Recovery")]
    [SerializeField, Min(0f)]
    private float _postAttackRecoveryDuration = 1.5f;

    [Header("グループ設定")]
    [SerializeField, Min(0f)]
    private float _groupMoveRadius = 3f;

    [Header("Group Follow")]
    [Tooltip("攻撃役の後ろへ並ぶ間隔")]
    [SerializeField, Min(0.1f)]
    private float _groupFollowDistance = 0.8f;

    [Tooltip("五角形の中心から左右頂点までの幅")]
    [SerializeField, Min(0.1f)]
    private float _groupFormationHalfWidth = 0.8f;

    [Tooltip("攻撃役の中心から後端までの距離")]
    [SerializeField, Min(0.1f)]
    private float _groupRearDistance = 1.5f;

    [Tooltip("追従位置で停止する距離")]
    [SerializeField, Min(0.01f)]
    private float _groupFollowStopDistance = 0.15f;

    private PostAttackStunBehaviour _postAttackStun;
    private GroupFollowBehaviour _groupFollowBehaviour;
    private EnemyGroup _group;
    private GroupPromotionBehaviour
    _groupPromotion;

    private bool _isGroupLeader;
}
