using Cysharp.Threading.Tasks;
using PixPlays.ElementalVFX;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GroundCrushSkill", menuName = "GameData/Skill/GroundCrushSkill")]

public class GroundCrush : SkillBase
{
    public float AttackRadius => _attackRadius;
    public override void Apply(ref AttackContext context)
    {
        float attackPower = context.AttackPower * _damageMultiplier;
        Transform playerTransform = context.PlayerTransform;

        Vector3 center =
            playerTransform.position +
            playerTransform.forward * _range;

        center = GetGroundPosition(center);

        context.OnAfterAttack += () =>
            ActivationGroundCrush(center, playerTransform.forward, attackPower).Forget();

        context.OnAfterAttack += () =>
            SpawnEffect(center, playerTransform.forward).Forget();
    }

    public override bool CanApply(AttackContext context, AttackData data)
    {
        bool isWarrior = context.PlayerMode == _isPlayerMode;

        bool isTargetAttackType = data.AttackType == _attackType;

        bool isComboCount = data.ComboIndex >= _getComboCount;

        bool isLastCombo = data.NextComboAttackId == -1;

        return isWarrior
            && isTargetAttackType
            && isComboCount
            && isLastCombo;
    }

    [SerializeField] private int _getComboCount = 2;                                       //コンボの何段目に実行するか
    [SerializeField] private float _attackRadius = 2.5f;                                   //攻撃範囲
    [SerializeField] private float _damageMultiplier = 1.8f;                               //与えるダメージ
    [SerializeField] private AttackType _attackType = AttackType.LightAttack;              //攻撃タイプ
    [SerializeField] private PlayerMode _isPlayerMode = PlayerMode.Warrior;                //プレイヤーが闘神モードかどうか
    [SerializeField] private float _delay = 1.0f;                                          //ヒットから発動までの待機時間
    [SerializeField] private float _range = 1;                                          // 攻撃の中心とPlayerとの距離
    [SerializeField] private float _knockBackPower;                                        //ノックバックの強さ
    [SerializeField] private float _knockBackUpward;                                       //ノックバックの角度
    [SerializeField] private string _effectKey;                                     //GroundSmashEffectを持つPrefab

    /// <summary>
    /// 攻撃の中心位置を地面に合わせる
    /// </summary>
    private Vector3 GetGroundPosition(Vector3 position)
    {
        position.y = 0f;
        return position;
    }

    /// <summary>
    /// スキルの処理
    /// </summary>
    /// <param name="playerTransform">攻撃発生位置</param>
    /// <param name="attackPower">攻撃力</param>
    /// <returns></returns>
    private async UniTask ActivationGroundCrush(
    Vector3 center,
    Vector3 forward,
    float attackPower)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(_delay));

        EnemyManager enemyManager = ServiceLocator.Get<EnemyManager>();

        IReadOnlyList<IEnemy> enemies =
            enemyManager.GetEnemiesInRange(center, _attackRadius);

        foreach (IEnemy enemy in enemies)
        {
            enemy.TakeDamage(new DamageContext
            {
                AttackPower = attackPower,
                PlayerMode = _isPlayerMode,
                IsCritical = false,
                CriticalMultiplier = 1f,

                Knockback = new KnockbackContext
                {
                    Direction = forward,
                    Power = _knockBackPower,
                    Upward = _knockBackUpward,
                }
            });
        }
    }

    /// <summary>
    /// Effectの生成
    /// </summary>
    /// <param name="position">生成位置</param>
    /// <returns></returns>
    private async UniTask SpawnEffect(
        Vector3 position,
        Vector3 forward)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(_delay));

        if (string.IsNullOrEmpty(_effectKey))
            return;

        if (ServiceLocator.TryGet(out EffectManager effectManager))
        {
            // エフェクトのスケールを攻撃範囲に合わせる
            effectManager.PlayEffect(
                _effectKey,
                position,
                Vector3.one * _attackRadius * 2f);
        }
    }
}
