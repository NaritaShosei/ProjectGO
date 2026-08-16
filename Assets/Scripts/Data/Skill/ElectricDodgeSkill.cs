using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ElectricDodgeSkill", menuName = "GameData/Skill/ElectricDodgeSkill")]

public class ElectricDodgeSkill : SkillBase
{
    public override void OnAcquire(IPlayerStats stats)
    {
        stats.OnJustDodgeSuccess += SaveApproveStatus;
        stats.OnEndDodge += ActivateElectricDodge;

        _attackPower = stats.AttackPower;
    }

    [SerializeField] private PlayerMode _isPlayerMode = PlayerMode.Thunder;    //雷神モードかどうか
    [SerializeField] private float _damageMultiplier = 0.8f;                   //ダメージ倍率
    [SerializeField] private float _grantEffectProbability = 0.5f;             //感電付与確率
    [SerializeField] private float _durationEffect = 3f;                       //感電持続時間
    [SerializeField] private float _upDamagePercentage = 0.8f;                 //感電中のダメージ上昇率                      
    [SerializeField] private float _attackRadius = 3f;                         //攻撃半径
    [SerializeField] private int _delay = 0;                                   //エフェクト生成タイミング
    [SerializeField] private int _duration = 3;                                //エフェクト持続時間
    [SerializeField] private Vector3 _scale = new(1, 1, 1);                    //エフェクトサイズ
    [SerializeField] private GameObject _effect;                               //スキルのエフェクト

    private float _attackPower;                                                //攻撃力
    private bool _isCan;

    private void SaveApproveStatus()    
    {
        _isCan = true;
    }

    private void ActivateElectricDodge(Transform playerTransform)
    {
        if (!_isCan) return;
        _isCan = false;

        UniTask.Delay(_delay);

        SpawnEffect(playerTransform.position, _duration, _scale);

        EnemyManager enemyManager = ServiceLocator.Get<EnemyManager>();

        IReadOnlyList<IEnemy> enemies = enemyManager.GetEnemiesInRange(playerTransform.position, _attackRadius);

        foreach (IEnemy hitEnemy in enemies)
        {
            hitEnemy.TakeDamage(new DamageContext
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
    }

    private async UniTask SpawnEffect(Vector3 pos, int durationTime, Vector3 scale)
    {
        GameObject effect = Instantiate(_effect, pos, Quaternion.identity);
        effect.transform.localScale = scale;

        await UniTask.Delay(durationTime);

        Destroy(effect);
    }
}
