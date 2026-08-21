using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(fileName = "ElectricDodgeSkill", menuName = "GameData/Skill/ElectricDodgeSkill")]

public class ElectricDodgeSkill : SkillBase
{
    public override void OnAcquire(IPlayerStats stats)
    {
        stats.OnJustDodgeSuccess += SaveApproveStatus;
        stats.OnEndDodge += ActivateElectricDodge;

        _playerStats = stats;
    }

    [SerializeField] private float _damageMultiplier = 0.8f;                   //ダメージ倍率
    [SerializeField] private float _grantEffectProbability = 0.5f;             //感電付与確率
    [SerializeField] private float _durationEffect = 3f;                       //感電持続時間
    [SerializeField] private float _upDamagePercentage = 0.8f;                 //感電中のダメージ上昇率                      
    [SerializeField] private float _attackRadius = 3f;                         //攻撃半径
    [SerializeField] private int _delay = 0;                                   //エフェクト生成タイミング
    [SerializeField] private int _duration = 3;                                //エフェクト持続時間
    [SerializeField] private Vector3 _scale = new(1, 1, 1);                    //エフェクトサイズ
    [SerializeField] private Vector3 _effectOffset = Vector3.zero;             //エフェクトの生成位置オフセット
    [SerializeField] private string _effectKey;                                //スキルのエフェクト

    private IPlayerStats _playerStats;
    private bool _isCan;

    private void SaveApproveStatus()
    {
        _isCan = true;
    }

    private void ActivateElectricDodge(Transform playerTransform)
    {
        if (!_isCan) return;
        _isCan = false;

        ActivateElectricDodgeAsync(playerTransform).Forget();
    }

    private async UniTask ActivateElectricDodgeAsync(Transform playerTransform)
    {
        CancellationToken token = playerTransform.GetCancellationTokenOnDestroy();

        Vector3 effectPos = playerTransform.position + _effectOffset;

        await UniTask.Delay(_delay, cancellationToken: token);

        SpawnEffect(effectPos, _scale);

        HashSet<IEnemy> hittedEnemies = new HashSet<IEnemy>();
        float elapsed = 0;

        while (elapsed < _duration)
        {
            EnemyManager enemyManager = ServiceLocator.Get<EnemyManager>();
            IReadOnlyList<IEnemy> enemies = enemyManager.GetEnemiesInRange(effectPos, _attackRadius);

            foreach (IEnemy hitEnemy in enemies)
            {
                if (!hittedEnemies.Add(hitEnemy)) continue;

                hitEnemy.TakeDamage(new DamageContext
                {
                    AttackPower = _playerStats.AttackPower * _damageMultiplier,
                    PlayerMode = PlayerMode.Thunder,
                    ElectricShock = new ElectricShock
                    {
                        GrantEffectProbability = _grantEffectProbability,
                        DurationEffect = _durationEffect,
                        UpDamagePercentage = _upDamagePercentage
                    }
                });
            }

            elapsed += Time.deltaTime;
            await UniTask.Yield(token);
        }
    }

    private void SpawnEffect(Vector3 pos, Vector3 scale)
    {
        if (ServiceLocator.TryGet(out EffectManager effectManager))
        {
            effectManager.PlayEffect(_effectKey, pos, scale);
        }
    }
}
