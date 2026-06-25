using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttackExecutor : MonoBehaviour
{
    /// <summary> ヒット結果をサウンドハンドラーなどに通知するイベント </summary>
    public event Action<HitSoundContext> OnHitResultReady;

    /// <summary> スイング音通知用。攻撃判定が出る瞬間に発火する </summary>
    public event Action<PlayerMode> OnSwingReady;
    public event Action OnHitConfirmed;

    public void Init(IPlayerStats stats, SkillManager manager)
    {
        _playerStats = stats;
        _skillManager = manager;
    }

    public void Execute(AttackData attackData, AttackInput attackInput, ModeData modeData)
    {
        var variantData = attackData.GetVariant(attackInput.ChargeLevel);

        if (variantData == null)
        {
            while (variantData == null && attackInput.ChargeLevel > ChargeLevel.None)
            {
                attackInput.ChargeLevel--;
                variantData = attackData.GetVariant(attackInput.ChargeLevel);
            }
            if (variantData == null)
            {
                Debug.LogError($"AttackData {attackData.name}に有効なバリアントが見つかりませんでした。攻撃を実行できません。");
                return;
            }
            Debug.LogWarning($"ChargeLevel {attackInput.ChargeLevel}のバリアントが見つかりませんでした。代わりにChargeLevel {attackInput.ChargeLevel}のバリアントを使用します。");
        }

        OnSwingReady?.Invoke(attackData.Mode);

        var attackPos = transform.position + transform.forward * variantData.AttackRange;
        var cols = Physics.OverlapSphere(attackPos, variantData.AttackRadius, _layer);

        Debug.Log($"{attackData.Mode}：{variantData.AttackName}で攻撃");

        var context = new AttackContext(attackData.Mode, _playerStats, attackPos, transform)
        {
            AttackPower = _playerStats.AttackPower * variantData.DamageMultiplier * modeData.AttackMultiplier,
        };

        if (variantData.EnableKnockback)
        {
            context.Knockback = new KnockbackContext
            {
                Direction = transform.forward,
                Power = variantData.KnockbackPower,
                Upward = variantData.KnockbackUpward
            };
        }

        var applicableSkills = GetApplicableSkills(context, attackData);
        ApplySkills(ref context, applicableSkills);
        context.OnBeforeAttack?.Invoke();

        bool hasHitResult = false;
        bool isWeakPoint = false;
        bool isArmorBreak = false;
        bool isKill = false;
        bool isArmorHit = false;
        var hitEnemyTargets = new List<ISpeedChange>();

        foreach (var col in cols)
        {
            if (!col.TryGetComponent(out IEnemy enemy)) continue;

            var perHitContext = context;
            RollCritical(ref perHitContext, modeData);
            perHitContext.OnHit?.Invoke(enemy);

            var damageContext = BuildDamageContext(perHitContext);

            damageContext.OnHitResult = result =>
            {
                hasHitResult = true;
                if (result.IsWeakPoint) isWeakPoint = true;
                if (result.IsArmorBreak) isArmorBreak = true;
                if (result.IsKill) isKill = true;
                if (result.IsArmorHit) isArmorHit = true;
                if (enemy is ISpeedChange speedChange)
                    hitEnemyTargets.Add(speedChange);
            };

            enemy.TakeDamage(damageContext);

            if (attackData.Mode == PlayerMode.Thunder && variantData.HasAdditionalLightningDamage)
            {
                var captured = enemy;
                var lightningPower = perHitContext.AttackPower;
                ExecuteLightningDamageAsync(captured, lightningPower, attackData.Mode, variantData.AdditionalLightningDamages).Forget();
            }
        }

        // 地面ヒット音（特定攻撃のみ）
        // if (variantData.PlayGroundHitSE)
        // Sound.PlaySE(gameObject, SoundCueNames.GroundHit);
        // TODO:地面ヒットSEが追加されたら対応

        if (hasHitResult)
        {
            OnHitConfirmed?.Invoke();

            if (ServiceLocator.TryGet(out CameraManager cameraManager))
                cameraManager.ExecutionCameraShake(variantData.CameraShakeData).Forget();

            // HitStop
            if (ServiceLocator.TryGet(out HitStopManager hitStop))
            {
                hitStop.Trigger(
                    data: variantData.HitStopData,
                    isWeakPoint: isWeakPoint,
                    isArmorBreak: isArmorBreak,
                    isKill: isKill,
                    hitEnemyTargets: hitEnemyTargets
                );
            }

            // サウンド通知
            OnHitResultReady?.Invoke(new HitSoundContext
            {
                IsKill = isKill,
                IsArmorBreak = isArmorBreak,
                IsWeakPoint = isWeakPoint,
                IsArmorHit = isArmorHit,
                PlayerMode = attackData.Mode,
            });
        }

        context.OnAfterAttack?.Invoke();
    }

    [Header("攻撃対象のレイヤー")]
    [SerializeField] private LayerMask _layer;
    [Header("Damage Popup")]
    [SerializeField] private Color _lightningDamagePopupColor = DamagePopupColorScope.LightningColor;
    private IPlayerStats _playerStats;
    private SkillManager _skillManager;

    private List<SkillBase> GetApplicableSkills(AttackContext context, AttackData data)
    {
        if (_skillManager == null) return new List<SkillBase>();
        return _skillManager.GetAttackSkills()
            .Where(skill => skill.CanApply(context, data))
            .OrderByDescending(skill => skill.Priority)
            .ToList();
    }

    private void ApplySkills(ref AttackContext context, List<SkillBase> skills)
    {
        foreach (var skill in skills) skill.Apply(ref context);
    }

    private void RollCritical(ref AttackContext context, ModeData data)
    {
        context.IsCritical = false;
        context.CriticalMultiplier = 1f;
        if (UnityEngine.Random.value < _playerStats.CriticalRate)
        {
            context.IsCritical = true;
            context.CriticalMultiplier = data.CriticalDamageMultiplier;
        }
    }

    private DamageContext BuildDamageContext(AttackContext context)
    {
        return new DamageContext
        {
            AttackPower = context.AttackPower,
            PlayerMode = context.PlayerMode,
            CriticalMultiplier = context.CriticalMultiplier,
            IsCritical = context.IsCritical,
            ElectricShock = context.ElectricShock,
            Knockback = context.Knockback
        };
    }

    /// <summary>
    /// ディレイ後に雷追加ダメージを与える
    /// </summary>
    private async UniTaskVoid ExecuteLightningDamageAsync(
        IEnemy enemy,
        float power,
        PlayerMode mode,
        AdditionalLightningDamageData[] datas)
    {
        foreach (var data in datas)
        {

            if (data.LightningDamageDelay > 0f)
                await UniTask.Delay(
                    TimeSpan.FromSeconds(data.LightningDamageDelay),
                    cancellationToken: destroyCancellationToken
                );

            if (enemy == null || enemy.IsDead) return;

            // このTakeDamage内で生成されるダメージポップアップだけ雷色にする。
            using (DamagePopupColorScope.Use(_lightningDamagePopupColor))
            {
                enemy.TakeDamage(new DamageContext
                {
                    AttackPower = power * data.LightningDamageMultiplier,
                    PlayerMode = mode,
                    IsCritical = false,
                    CriticalMultiplier = 1f,
                });
            }
        }
    }
}

/// <summary>
/// Playerの攻撃やスキルに扱う情報
/// </summary>
public struct AttackContext
{
    public float AttackPower;
    public readonly PlayerMode PlayerMode;
    public readonly IPlayerStats PlayerStats;

    // 攻撃の座標
    public readonly Vector3 AttackPosition;
    public readonly Transform PlayerTransform;

    public bool IsCritical;
    public float CriticalMultiplier;

    public KnockbackContext? Knockback;

    /// <summary>攻撃開始直前</summary>
    public Action OnBeforeAttack;

    /// <summary>攻撃終了直後</summary>
    public Action OnAfterAttack;

    /// <summary>敵にヒットした瞬間</summary>
    public Action<IEnemy> OnHit;

    /// <summary>感電</summary>
    public ElectricShock ElectricShock;

    public AttackContext(PlayerMode mode, IPlayerStats playerStats, Vector3 attackPos, Transform playerTransform)
    {
        PlayerMode = mode;
        PlayerStats = playerStats;
        AttackPosition = attackPos;
        PlayerTransform = playerTransform;

        AttackPower = 0;
        IsCritical = false;
        CriticalMultiplier = 1f;
        Knockback = null;
        OnBeforeAttack = null;
        OnAfterAttack = null;
        OnHit = null;
        ElectricShock = new();
    }
}

/// <summary>
/// Enemyが攻撃を受ける際に扱う情報
/// </summary>
public struct DamageContext
{
    public float AttackPower;
    public PlayerMode PlayerMode;

    public bool IsCritical;
    public float CriticalMultiplier;

    public ElectricShock ElectricShock;
    /// <summary>
    /// HasValueで攻撃にノックバック効果があるかを確認。
    /// Valueでノックバックの方向や威力を取得可能。
    /// </summary>
    public KnockbackContext? Knockback;

    /// <summary>
    /// 攻撃がヒットした瞬間に発動するイベント。HitResultでヒットの結果を受け取ることができる。
    /// </summary>
    public Action<HitResult> OnHitResult;
}

/// <summary>
/// ノックバックの方向や威力などの情報
/// </summary>
public struct KnockbackContext
{
    public Vector3 Direction;
    public float Power;
    public float Upward;
}

/// <summary>
/// 攻撃がヒットした際の結果に関する情報
/// </summary>
public struct HitResult
{
    public bool IsKill;
    public bool IsArmorBreak;
    public bool IsWeakPoint;
    public bool IsArmorHit;
}

/// <summary>
/// 攻撃のヒット結果をサウンドハンドラーなどに通知するための情報
/// </summary>
public struct HitSoundContext
{
    public bool IsKill;
    public bool IsArmorBreak;
    public bool IsWeakPoint;
    public bool IsArmorHit;
    public PlayerMode PlayerMode;
}
