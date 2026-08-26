using DG.Tweening;
using UnityEngine;

public class ShieldDraugr : MobEnemy
{
    public float CurrentShieldDurability => _currentShieldDurability;

    public float MaxShieldDurability => _shieldDurability;

    public bool IsShieldBroken => _shieldState == ShieldState.Broken;


    private enum ShieldState { Guarding, Broken }

    [Header("盾持ちドラウグル専用パラメータ")]
    [SerializeField, Tooltip("盾の耐久値")] private float _shieldDurability = 100f;
    [SerializeField, Range(-1f, 1f), Tooltip("盾で受け止める範囲")] private float _frontalDotThreshold = 0.5f; // 前方約60度以内
    [SerializeField] private Transform _shieldEffectPoint;
    [SerializeField] private GameObject _shieldObject;
    [SerializeField, Tooltip("盾破壊時のエフェクト")] private string _shieldBrokenEffect = "shieldBrokenEffect";
    [SerializeField, Tooltip("盾被ダメージエフェクト")] private string _shieldDamageEffect = "shieldDamageEffect";
    [SerializeField, Tooltip("盾破壊時のエフェクトの大きさ")] private Vector3 _shieldBrokenEffectScale;
    [SerializeField, Tooltip("こぶし攻撃の確率"),Range(0f,1f)] private float _fistAttackChance = 0.1f;
    [SerializeField, Tooltip("こぶし攻撃の抽選間隔（秒）")] private float _fistRerollInterval = 2f;
    [SerializeField, Tooltip("攻撃後硬直の時間")] private float _postAttackRecoveryDuration = 5f;
    [SerializeField] private EnemyAttackPattern _fistAttackPattern;

    private float _fistRerollTimer = 0f;

    private ShieldState _shieldState = ShieldState.Guarding;
    //現在の盾の耐久値
    private float _currentShieldDurability;
    private const int ShieldLayerIndex = 1;

    [SerializeField, Tooltip("盾構え解除にかかる時間")]
    private float _shieldAnimationBlendDuration = 0.3f;

    private Tween _shieldAnimationTween;
    private PostAttackStunBehaviour _postAttackStun;
    private EffectManager _effectManager;


    public override void Init()
    {
        base.Init();
        _effectManager = ServiceLocator.Get<EffectManager>();
    }

    public override void ReInitialize(Vector3 spawnPosition)
    {
        base.ReInitialize(spawnPosition);
        _currentShieldDurability = _shieldDurability;
        _shieldState = ShieldState.Guarding;
        _fistRerollTimer = 0f;

        _shieldObject.SetActive(true);
        ResetShieldAnimation();
    }

    protected override void RegisterBehaviours(BehaviourInitContext initCtx)
    {
        base.RegisterBehaviours(initCtx);

        _postAttackStun = new PostAttackStunBehaviour(_postAttackRecoveryDuration, HandlePostAttackStunExit);
        _postAttackStun.Init(initCtx);
        _runner.Register(_postAttackStun);
        _attack.OnAttackFinished += HandleAttackFinished;
    }

    public override void TakeDamage(DamageContext context)
    {
        if (_isDead || !CanTakeDamage) return;

        int damage = DamageSystem.CalculateDamage(context, _defenceContext);
        bool isBattleGod = context.PlayerMode == PlayerMode.Warrior;
        bool isFrontal = IsFrontalHit();

        bool appliedToHp = false;
        bool appliedToShield = false;
        bool didBreakThisHit = false;
        bool wasBlocked = false;
        //現在使用なし正面ガード時に使用

        if (_shieldState == ShieldState.Broken)
        {
            Debug.Log("生身ダメージ");
            // 生身：常時HPへ通す
            _stats.TakeDamage(damage);
            appliedToHp = true;
        }
        else if (isFrontal)
        {
            if (isBattleGod)
            {
                Debug.Log("盾にダメージ");
                ApplyShieldDamage(damage);
                appliedToShield = true;
                didBreakThisHit = _shieldState == ShieldState.Broken;

                if (!didBreakThisHit)
                {
                    _enemyAnimator.ShieldBlockHitTrigger();
                }
            }
            else
            {
                Debug.Log("正面につきダメージ無効");
                wasBlocked = true;
                _enemyAnimator.ShieldBlockHitTrigger();
            }
            // 通常モード＋正面 → 無効（何もしない）
        }
        else
        {
            Debug.Log("背面攻撃");
            // 背面・側面 → 常にHPへ通す
            _stats.TakeDamage(damage);
            appliedToHp = true;
        }

        bool willKill = appliedToHp && _stats.CurrentHealth <= 0;

        InvokeOnDamageDealt(damage, isWeakPoint: isBattleGod && appliedToShield, context.IsCritical);

        context.OnHitResult?.Invoke(new HitResult
        {
            IsKill = willKill,
            IsArmorBreak = didBreakThisHit,
            IsWeakPoint = isBattleGod && appliedToShield,
            IsArmorHit = appliedToShield,
        });

        if (appliedToHp && !willKill) InvokeOnDamaged();

        // 完全ガードされた攻撃には追加効果を適用しない
        if (!wasBlocked && !appliedToShield)
        {
            ApplyAdditionalEffects(context);
        }

        Debug.Log(
    $"[ShieldDraugr] " +
    $"State={_shieldState}, " +
    $"Shield={_currentShieldDurability}/{_shieldDurability}, " +
    $"HP={_stats.CurrentHealth}"
);
    }

    protected override void UpdateEnemy(float deltaTime)
    {
        if (IsShieldBroken)
        {
            TickFistAttackGate(deltaTime);
        }

        base.UpdateEnemy(deltaTime);
    }

    private void TickFistAttackGate(float deltaTime)
    {
        _fistRerollTimer -= deltaTime;

        if (_fistRerollTimer > 0f) return;

        _fistRerollTimer = _fistRerollInterval;

        if (Random.value < _fistAttackChance)
        {
            // 成功：既にクールダウン中でなければ即攻撃可能にする
            if (AttackCooldownRemaining > 0f) AttackCooldownRemaining = 0f;
        }
        else
        {
            // 失敗：次のロールまで攻撃をブロックする
            // （SelectedPattern自体は非nullのままなのでApproachは動き続ける）
            AttackCooldownRemaining = _fistRerollInterval;
        }
    }

    private bool IsFrontalHit()
    {
        Vector3 toPlayer = _playerTransform.position - transform.position;
        toPlayer.y = 0f;
        toPlayer = toPlayer.normalized;
        return Vector3.Dot(transform.forward, toPlayer) >= _frontalDotThreshold;
    }

    private void ApplyShieldDamage(int damage)
    {
        _currentShieldDurability = Mathf.Max(0f, _currentShieldDurability - damage);

        if(IsShieldBroken)
        {
            //岩を砕くエフェクト通知
            _effectManager.PlayEffect(_shieldDamageEffect, _shieldEffectPoint.position, _shieldBrokenEffectScale);
        }

        if (_currentShieldDurability <= 0f)
        {
            BreakShield();
        }

       
    }

    /// <summary>
    /// 盾持ちから生身に変わる
    /// </summary>
    private void BreakShield()
    {
        if (_shieldState == ShieldState.Broken)
            return;

        _shieldState = ShieldState.Broken;
        //盾破壊通知
        InvokeOnArmorBroken();

        //盾破壊アニメーション開始
        _enemyAnimator.ShieldBreakTrigger();
        //盾は破壊エフェクト
        _effectManager.PlayEffect(_shieldBrokenEffect, _shieldEffectPoint.position, _shieldBrokenEffectScale);
        //盾のモデル非表示
        HideShield();
        //上半身盾構え解除
        SetShieldAnimation(false);
        // 現在のBehaviourを終了
        _runner.ForceExitAction();
        Debug.Log("[ShieldDraugr] Shield Broken!");
    }

    /// <summary>
    /// 盾非表示
    /// </summary>
    private void HideShield()
    {
        _shieldObject.SetActive(false);
    }

    private void SetShieldAnimation(bool enabled)
    {
        float targetWeight = enabled ? 1f : 0f;

        _shieldAnimationTween?.Kill();

        _shieldAnimationTween = DOTween.To(() => _animator.GetLayerWeight(ShieldLayerIndex), weight => _animator.SetLayerWeight(ShieldLayerIndex, weight), targetWeight, _shieldAnimationBlendDuration);
    }

    private void ResetShieldAnimation()
    {
        _shieldAnimationTween?.Kill();
        _animator.SetLayerWeight(ShieldLayerIndex, 1f);
    }

    protected override EnemyAttackPattern SelectPattern()
    {
        if (!IsShieldBroken)
        {
            return base.SelectPattern();
        }

        return _fistAttackPattern;
    }

    private void HandleAttackFinished()
    {
        if (IsShieldBroken) return;

        _turn.SetOverrideDirection(transform.forward);

        _runner.ForceBehaviour(_postAttackStun);
    }

    private void HandlePostAttackStunExit()
    {
        _turn.SetOverrideDirection(null);
    }

    private void OnDisable()
    {
        _shieldAnimationTween?.Kill();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_attack != null)
        {
            _attack.OnAttackFinished -= HandleAttackFinished;
        }
    }
}
