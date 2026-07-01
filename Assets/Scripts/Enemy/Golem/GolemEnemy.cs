using UnityEngine;
using System;

/// <summary>
/// ゴーレム専用Enemy
/// 鎧破壊時のダウン・復帰・威嚇行動を持つ
/// </summary>
public class GolemEnemy : MobEnemy, IFormationParticipant
{
    /// <summary>
    /// ゴーレムの初期化
    /// Behaviour・Condition・Armorを生成して登録する
    /// </summary>
    public override void Init()
    {
        base.Init();

        _blinkEffect = new BlinkEffect(_bodyRenderer,_blinkSpeed);
        _effectManager = ServiceLocator.Get<EffectManager>();

        OnArmorBroken += HandleArmorBroken;

        if(_attack != null)
        {
            _attack.OnAttackFinished += HandlePostAttack;
        }
    }

    /// <summary>
    /// ゴーレム専用ダメージ処理
    ///
    /// 通常時
    /// ・鎧へダメージ
    /// Down中
    /// ・本体HPへダメージ
    /// </summary>
    public override void TakeDamage(DamageContext context)
    {
        bool isDown = ConditionController.HasCondition(ConditionType.Down);

        if (_isDead)
        {
            return;
        }

        int damage = DamageSystem.CalculateDamage(context, _defenceContext);

        int showDamage = damage;

        if (!isDown && _defenceContext.EnemyType == EnemyDefenceType.Armor && _armor != null)
        {
            bool armorWasAlive =
            _defenceContext.EnemyType == EnemyDefenceType.Armor;

            _armor.AbsorbDamageAndReturnExcess(damage);

            bool isArmorBreak =
            armorWasAlive && _defenceContext.EnemyType == EnemyDefenceType.Flesh;

            bool isWeak = context.PlayerMode == PlayerMode.Warrior;

            InvokeOnDamageDealt(
                showDamage,
                isWeak,
                context.IsCritical);

            InvokeOnHitEffect(new HitEffectContext
            {
                Position = transform.position,
                PlayerMode = context.PlayerMode,
                IsArmorHit = !isArmorBreak,
                IsArmorBreak = isArmorBreak
            });

            context.OnHitResult?.Invoke(
                new HitResult
                {
                    IsKill = false,
                    IsArmorBreak = isArmorBreak,
                    IsWeakPoint = isWeak,
                    IsArmorHit = !isArmorBreak,
                });

            if (!isArmorBreak)
            {
                InvokeOnDamaged();
            }
            return;
        }

        // Down中、または鎧なし/生身状態は本体HPへ
        bool isWeakPoint = context.PlayerMode == PlayerMode.Thunder;

        InvokeOnDamageDealt(showDamage, isWeakPoint, context.IsCritical);

        _stats.TakeDamage(damage);

        bool willKill = _stats.CurrentHealth <= 0;


        context.OnHitResult?.Invoke(
            new HitResult
            {
                IsKill = willKill,
                IsArmorBreak = false,
                IsWeakPoint = isWeakPoint,
                IsArmorHit = false,
            });

        if (!willKill)
        {
            InvokeOnDamaged();
        }

        if (willKill)
        {
            _stats.Kill();
        }
    }

    /// <summary>
    /// ダウン終了後に鎧を復元する
    /// </summary>
    public void RecoverArmor()
    {
        _blinkEffect.StopBlink();

        if (_armor == null)
        {
            Debug.LogWarning("Armor is null.");
            return;
        }

        _armor.Restore();
        RebindArmor();
        _defenceContext.EnemyType = EnemyDefenceType.Armor;
        InvokeArmorRegistered();
    }

    [Header("Down Settings")]
    [SerializeField, Tooltip("鎧破壊後にダウン状態を維持する時間（秒）")]
    private float _downDuration = 5f;

    [Header("Blink Effect")]

    [SerializeField, Tooltip("鎧破壊中に点滅させるRenderer")]
    private Renderer[] _bodyRenderer;

    [SerializeField, Tooltip("点滅速度")]
    private int _blinkSpeed = 100;

    [Header("Combat Settings")]
    [SerializeField, Tooltip("ゴーレム専用の攻撃クールダウン（0以下ならAttackPattern設定を使用）")]
    private float _attackCooldownOverride = 5f;

    [SerializeField, Range(0f, 1f), Tooltip("攻撃後に威嚇へ移行する確率")] private float _barkChance = 0.5f;

    [SerializeField]
    private Transform _attackEffectPoint;

    [SerializeField]
    private String _attackEffecktText;

    private BlinkEffect _blinkEffect;
    private EffectManager _effectManager;

    /// <summary>
    /// オブジェクト破棄時にイベント購読を解除し、BlinkEffectを停止する
    /// </summary>
    protected override void OnDestroy()
    {
        OnArmorBroken -= HandleArmorBroken;

        if (_attack != null)
        {
            _attack.OnAttackFinished -= HandlePostAttack;
        }

        _blinkEffect?.StopBlink();

        base.OnDestroy();
    }

    protected override void RegisterBehaviours(BehaviourInitContext initCtx)
    {
        // TurnProfileが未設定の場合は警告を出してTurnを登録しない
        if (_turnProfile == null)
        {
            Debug.LogWarning($"{nameof(GolemEnemy)}: TurnProfileが未設定です。Turnは無効になります。");
        }
        else
        {
            _turn = new TurnBehaviour(_turnProfile);
            _turn.Init(initCtx);
            _runner.RegisterTurn(_turn);
        }

        // AttackerSlotが未設定の場合は警告を出してAttackを登録しない
        if (_services.AttackerSlot == null)
        {
            Debug.LogWarning($"{nameof(GolemEnemy)}: AttackerSlotが未注入です。Attackは無効になります。");
        }
        else if (_data.AttackPatterns == null || _data.AttackPatterns.Count == 0)
        {
            Debug.LogWarning($"{nameof(GolemEnemy)}: AttackPatternsが空です。Attack・スロット取得をスキップします。");
        }
        else
        {
            _attack = new MeleeAttackBehaviour(
                _services,
                _animator,
                _distanceProfile,
                _attackCooldownOverride);

            _attack.Init(initCtx);
            _runner.Register(_attack);

            _services.AttackerSlot.TryAcquire(Id, 1);

            if (_distanceProfile != null)
            {
                _bark = new BarkBehaviour(
                    _distanceProfile,
                    _services,
                    _data.BarkChance,
                    true);

                _bark.Init(initCtx);
                _runner.Register(_bark);
            }
        }

        if (_distanceProfile == null)
        {
            Debug.LogWarning($"{nameof(GolemEnemy)}: DistanceProfileが未設定です。Approachは無効になります。");
        }
        else
        {
            var move = new ApproachBehaviour(
                _distanceProfile,
                _services);

            move.Init(initCtx);
            _runner.Register(move);
        }
    }

    /// <summary>
    /// 攻撃終了時処理
    /// 確率で威嚇Behaviourを強制実行する
    /// </summary>
    private void HandlePostAttack()
    {
        if (_bark == null)
            return;

        if (UnityEngine.Random.value < _barkChance)
        {
            _runner.ForceBehaviour(_bark);
        }
    }

    protected override void HandleAttackEffect()
    {
        if (_effectManager == null) return;
        Vector3 pos = _attackEffectPoint != null ? _attackEffectPoint.position : transform.position;
        _effectManager.PlayEffect(_attackEffecktText, pos);
    }

    /// <summary>
    /// 鎧破壊時処理
    /// 点滅演出開始とDownConditionを付与する
    /// </summary>
    private void HandleArmorBroken(IEnemy enemy)
    {
        _blinkEffect.StartBlink();

        ConditionController.ApplyCondition(
            new DownCondition(_downDuration));
    }

    /// <summary>
    /// 鎧破壊完了処理
    /// 防御タイプをFleshへ変更する
    /// </summary>
    private void BreakArmor()
    {
        _defenceContext.EnemyType = EnemyDefenceType.Flesh;
        _armor.OnBroken -= BreakArmor;
        InvokeOnArmorBroken();
    }
}
