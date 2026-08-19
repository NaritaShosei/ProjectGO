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
    [SerializeField, Tooltip("盾破壊時のエフェクトの大きさ")] private Vector3 _shieldBrokenEffectScale;

    private ShieldState _shieldState = ShieldState.Guarding;
    //現在の盾の耐久値
    private float _currentShieldDurability;
    private const int ShieldLayerIndex = 1;

    [SerializeField, Tooltip("盾構え解除にかかる時間")]
    private float _shieldAnimationBlendDuration = 0.3f;

    private Tween _shieldAnimationTween;

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

        _shieldObject.SetActive(true);
        ResetShieldAnimation();
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
            }
            else
            {
                Debug.Log("正面につきダメージ無効");
                wasBlocked = true;
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
        // TODO: 岩を砕くエフェクト通知
        //TODO:盾のダメージ演出
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
        //盾は破壊エフェクト
        _effectManager.PlayEffect(_shieldBrokenEffect, _shieldEffectPoint.position, _shieldBrokenEffectScale);
        //盾のモデル非表示
        HideShield();
        //上半身盾構え解除
        SetShieldAnimation(false);
        // 現在のBehaviourを終了
        _runner.ForceExitAction();
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
}
