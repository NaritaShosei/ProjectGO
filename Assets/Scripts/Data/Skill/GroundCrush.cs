using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GroundCrushSkill", menuName = "GameData/Skill/GroundCrushSkill")]

public class GroundCrush : SkillBase
{
    public override void Apply(ref AttackContext context)
    {
        float attackPower = context.AttackPower * _damageMagnification;
        Transform playerTransform = context.PlayerTransform;

        // OnAfterAttack に登録するだけでスキルになる
        context.OnAfterAttack += () => ActivationGroundCrush(playerTransform, attackPower);
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

    [SerializeField] private int _getComboCount = 2;                          //コンボの何段目に実行するか
    [SerializeField] private float _attackRadius = 2.5f;                      //攻撃範囲
    [SerializeField] private float _damageMagnification = 1.8f;               //与えるダメージ
    [SerializeField] private AttackType _attackType = AttackType.LightAttack; //攻撃タイプ
    [SerializeField] private PlayerMode _isPlayerMode = PlayerMode.Warrior;   //プレイヤーが闘神モードかどうか
    [SerializeField] private float _delay = 1.0f;                             //ヒットから発動までの待機時間
    [SerializeField] private GameObject _effectPrefab;                        //GroundSmashEffectを持つPrefab

    private async UniTaskVoid ActivationGroundCrush(Transform playerTransform, float attackPower)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(_delay));

        if (playerTransform == null) return;

        Collider[] enemies = Physics.OverlapSphere(playerTransform.position, _attackRadius);

        foreach (Collider col in enemies)
        {
            if (!col.TryGetComponent(out IEnemy enemy)) continue;

            enemy.TakeDamage(new DamageContext
            {
                AttackPower = attackPower,
                PlayerMode = _isPlayerMode,
                IsCritical = false,
                CriticalMultiplier = 1f,
                ElectricShock = new ElectricShock(),
                Knockback = null,
            });
        }

        Debug.Log($"{enemies.Length}体の敵に{attackPower}ダメージ!");
    }
}
