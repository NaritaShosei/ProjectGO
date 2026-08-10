using UnityEngine;

[CreateAssetMenu(fileName = "ElectricDodgeSkill", menuName = "GameData/Skill/ElectricDodgeSkill")]

public class ElectricDodgeSkill : SkillBase
{
    public override void OnAcquire(IPlayerStats stats)
    {
        stats.OnJustDodgeSuccess += SaveApproveStatus;
        stats.OnEndDodge += ActivationElectrickDodge;
    }

    public void ActivationElectrickDodge(Transform playerTransform)
    {
        if (_isCan) return;

        Collider[] hitEnemies = Physics.OverlapSphere(playerTransform.position, _attackRadius);
        foreach (Collider hitEnemy in hitEnemies)
        {
            if (!hitEnemy.TryGetComponent<IEnemy>(out var enemy)) continue;

            enemy.TakeDamage(new DamageContext
            {
                AttackPower = _attackPower * _damageMultiplier,
                PlayerMode = _isPlayerMode,
                ElectricShock = new ElectricShock
                {
                    GrantEffectProbability = _grantEffectProbability,
                    DurationEffect = _durationEffect,
                    UpDamagePercentage = _upDamagePercentage
                }
            });
        }
        _isCan = false;
    }

    [SerializeField] private PlayerMode _isPlayerMode = PlayerMode.Thunder;    //雷神モードかどうか
    [SerializeField] private float _attackPower = 100f;                        //攻撃力
    [SerializeField] private float _damageMultiplier = 0.8f;                   //ダメージ倍率
    [SerializeField] private float _grantEffectProbability = 0.5f;             //感電付与確率
    [SerializeField] private float _durationEffect = 3f;                       //感電持続時間
    [SerializeField] private float _upDamagePercentage = 0.8f;                 //感電中のダメージ上昇率                      
    [SerializeField] private float _attackRadius = 3f;                         //攻撃半径

    private bool _isCan;

    private bool SaveApproveStatus()    
    {
        _isCan = true;

        return _isCan;
    }
}
