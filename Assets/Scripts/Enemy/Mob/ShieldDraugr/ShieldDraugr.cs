using DG.Tweening;
using System;
using UnityEngine;

public class ShieldDraugr : MobEnemy,IArmorHealth
{
    public float CurrentShieldDurability => _currentShieldDurability;

    public float MaxShieldDurability => _shieldData.ShieldDurability;

    public bool IsShieldBroken => _shieldState == ShieldState.Broken;

    public event Action<float, float> OnShieldChanged;
    public event Action OnShieldBroken;

    float IArmorHealth.CurrentHealth => _currentShieldDurability;
    float IArmorHealth.MaxHealth => _shieldData.ShieldDurability;

    event Action<float, float> IArmorHealth.OnHealthChanged
    {
        add => OnShieldChanged += value;
        remove => OnShieldChanged -= value;
    }

    event Action IArmorHealth.OnBroken
    {
        add => OnShieldBroken += value;
        remove => OnShieldBroken -= value;
    }

    // MobEnemyの鎧解決を、盾が健在な間だけ自分自身に差し替える
    protected override IArmorHealth ActiveArmor => IsShieldBroken ? null : this;

    public override void Init()
    {
        base.Init();
        _effectManager = ServiceLocator.Get<EffectManager>();
    }

    public override void ReInitialize(Vector3 spawnPosition)
    {
        base.ReInitialize(spawnPosition);
        _currentShieldDurability = _shieldData.ShieldDurability;
        _shieldState = ShieldState.Guarding;
        _fistRerollTimer = 0f;

        _shieldObject.SetActive(true);
        ResetShieldAnimation();
        InvokeArmorRegistered();
    }

    public override void TakeDamage(DamageContext context)
    {
        if (_isDead || !CanTakeDamage) return;

        int damage = DamageSystem.CalculateDamage(context, _defenceContext);
        bool isWarrior = context.PlayerMode == PlayerMode.Warrior;
        bool isThunder = context.PlayerMode == PlayerMode.Thunder;
        bool isFrontal = IsFrontalHit();

        bool appliedToHp = false;
        bool appliedToShield = false;
        bool didBreakThisHit = false;
        bool wasBlocked = false;
        bool isThunderArmorHit = false;

        if (_shieldState == ShieldState.Broken)
        {
            Debug.Log("生身ダメージ");
            _stats.TakeDamage(damage);
            appliedToHp = true;
        }
        else if (isFrontal)
        {
            if (isWarrior)
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

                if (isThunder)
                {
                    isThunderArmorHit = true;
                }
            }
        }
        else
        {
            Debug.Log("背面攻撃");
            _stats.TakeDamage(damage);
            appliedToHp = true;
        }

        bool willKill = appliedToHp && _stats.CurrentHealth <= 0;

        //ダメージ表記
        if (appliedToShield)
        {
            // 闘神：盾への実ダメージを表示
            InvokeOnDamageDealt(
                damage,
                isWeakPoint: false,
                context.IsCritical);
        }
        else if (appliedToHp)
        {
            // 生身：通常通りダメージを表示
            InvokeOnDamageDealt(
                damage,
                isWeakPoint: isWarrior || isThunder,
                context.IsCritical);
        }
        else if (isThunder && isFrontal)
        {
            // 雷神：盾にはダメージを与えないが、0ダメージを表示
            InvokeOnDamageDealt(
                0,
                isWeakPoint: false,
                context.IsCritical);
        }

        if (appliedToHp)
        {
            InvokeOnHitEffect(
                new HitEffectContext
                {
                    Position = transform.position,
                    PlayerMode = context.PlayerMode,
                });
        }

        context.OnHitResult?.Invoke(new HitResult
        {
            IsKill = willKill,
            IsArmorBreak = didBreakThisHit,
            IsWeakPoint = (isWarrior || isThunder) && appliedToHp,
            IsArmorHit = (appliedToShield && !didBreakThisHit) || isThunderArmorHit,
        });


        if (appliedToHp && !willKill) InvokeOnDamaged();

        // 完全ガードされた攻撃には追加効果を適用しない
        if (!wasBlocked && !appliedToShield)
        {
            ApplyAdditionalEffects(context);
        }
    }

    public override void HandleSpawnEnd()
    {
        base.HandleSpawnEnd();
        SetShieldLayerWeight(1f);
    }

    /// <summary>
    /// 盾ゲージの表示アンカーを返す。未設定ならHPゲージと同じ位置にフォールバックする。
    /// </summary>
    public Transform GetShieldGaugeAnchor()
    {
        if (_shieldGaugeAnchor != null) return _shieldGaugeAnchor;

        Debug.LogWarning($"{name}: ShieldGaugeAnchorが未設定です。UIAnchorを使用します。", this);
        return GetUIAnchor();
    }

    private enum ShieldState { Guarding, Broken }

    [Header("盾持ちドラウグル専用パラメータ")]
    [SerializeField] private ShieldDraugrData _shieldData;

    [SerializeField] private Transform _shieldEffectPoint;
    [SerializeField] private GameObject _shieldObject;

    [SerializeField, Tooltip("盾ゲージ表示位置（盾オブジェクトの上あたりに配置）")]
    private Transform _shieldGaugeAnchor;

    private float _fistRerollTimer = 0f;

    private ShieldState _shieldState = ShieldState.Guarding;
    private float _currentShieldDurability;
    private const int ShieldLayerIndex = 1;

    [SerializeField, Tooltip("盾構え解除にかかる時間")]
    private float _shieldAnimationBlendDuration = 0.3f;

    private Tween _shieldAnimationTween;
    private PostAttackStunBehaviour _postAttackStun;
    private EffectManager _effectManager;

    protected override void RegisterBehaviours(BehaviourInitContext initCtx)
    {
        base.RegisterBehaviours(initCtx);

        _postAttackStun = new PostAttackStunBehaviour(_shieldData.PostAttackRecoveryDuration, HandlePostAttackStunExit);
        _postAttackStun.Init(initCtx);
        _runner.Register(_postAttackStun);

        if (_attack != null)
        {
            _attack.OnAttackFinished += HandleAttackFinished;
        }
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

        _fistRerollTimer = _shieldData.FistRerollInterval;

        if (UnityEngine.Random.value < _shieldData.FistAttackChance)
        {
            // 成功：既にクールダウン中でなければ即攻撃可能にする
            if (AttackCooldownRemaining > 0f) AttackCooldownRemaining = 0f;
        }
        else
        {
            // 失敗：次のロールまで攻撃をブロックする
            AttackCooldownRemaining = _shieldData.FistRerollInterval;
        }
    }

    private bool IsFrontalHit()
    {
        Vector3 toPlayer = _playerTransform.position - transform.position;
        toPlayer.y = 0f;
        toPlayer = toPlayer.normalized;
        return Vector3.Dot(transform.forward, toPlayer) >= _shieldData.FrontalDotThreshold;
    }

    private void ApplyShieldDamage(int damage)
    {
        _currentShieldDurability = Mathf.Max(0f, _currentShieldDurability - damage);

        OnShieldChanged?.Invoke(_currentShieldDurability, MaxShieldDurability);

        if (!IsShieldBroken)
        {
            //岩を砕くエフェクト通知
            _effectManager.PlayEffect(_shieldData.ShieldDamageEffect, _shieldEffectPoint.position, _shieldData.ShieldBrokenEffectScale);
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

        ClearSelectedPattern();

        //盾破壊通知
        InvokeOnArmorBroken();
        OnShieldBroken?.Invoke();

        //盾破壊アニメーション開始
        _enemyAnimator.ShieldBreakTrigger();

        //盾は破壊エフェクト
        _effectManager.PlayEffect(_shieldData.ShieldBrokenEffect, _shieldEffectPoint.position, _shieldData.ShieldBrokenEffectScale);

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

        return _shieldData.FistAttackPattern;
    }

    private void HandleAttackFinished()
    {
        if (IsShieldBroken) return;

        if (_turn != null)
        {
            _turn.SetOverrideDirection(transform.forward);
        }
        else
        {
            Debug.LogWarning($"{nameof(ShieldDraugr)}: TurnBehaviourが未登録です");
        }

        _runner.ForceBehaviour(_postAttackStun);
    }

    private void HandlePostAttackStunExit()
    {
        if (_turn == null) return;
        _turn.SetOverrideDirection(null);
    }

    private void SetShieldLayerWeight(float weight)
    {
        _shieldAnimationTween?.Kill();

        _animator.SetLayerWeight(ShieldLayerIndex, weight);
    }

    protected override void HandleSpawnEffect()
    {
        base.HandleSpawnEffect();

        SetShieldLayerWeight(0f);
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
