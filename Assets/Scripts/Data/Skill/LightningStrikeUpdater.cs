using System.Collections.Generic;
using UnityEngine;

public class LightningStrikeUpdater : ISkillUpdater
{

    public LightningStrikeUpdater(LightningStrike data)
    {
        _data = data;
        _timer = Random.Range(data.MinInterval, data.MaxInterval);
    }

    public void OnUpdate(float deltaTime, PlayerMode mode, IPlayerStats stats,
        Vector3 playerPosition, EnemyManager enemyManager)
    {
        if (mode != PlayerMode.Thunder) return;

        _timer -= deltaTime;
        if (_timer > 0f) return;

        _timer = Random.Range(_data.MinInterval, _data.MaxInterval);
        Trigger(stats, playerPosition, enemyManager);
    }

    private readonly LightningStrike _data;
    private float _timer;
    private void Trigger(IPlayerStats stats, Vector3 playerPosition, EnemyManager enemyManager)
    {
        var enemies = enemyManager.GetEnemiesInRange(playerPosition, _data.SearchRadius);
        if (enemies.Count == 0) return;

        var shuffled = new List<IEnemy>(enemies);
        shuffled.Shuffle();

        int targetCount = Mathf.Min(_data.TargetCount, shuffled.Count);

        var effects = _data.HitEffectPrefabs != null
            ? new List<GameObject>(_data.HitEffectPrefabs)
            : new List<GameObject>();
        effects.Shuffle();

        var alreadyHit = new HashSet<IEnemy>();

        for (int i = 0; i < targetCount; i++)
        {
            var target = shuffled[i];
            var areaTargets = enemyManager.GetEnemiesInRange(target.Position, _data.AreaRadius);

            foreach (var enemy in areaTargets)
            {
                // 複数ターゲットの範囲が重なっても重複ダメージを与えない
                if (!alreadyHit.Add(enemy)) continue;

                float damage = stats.AttackPower * _data.DamageMultiplier;
                var damageContext = new DamageContext
                {
                    AttackPower = damage,
                    PlayerMode = PlayerMode.Thunder,
                    ElectricShock = new ElectricShock
                    {
                        DurationEffect = _data.ElectricShockDuration,
                        GrantEffectProbability = _data.GrantEffectProbability,
                        UpDamagePercentage = _data.UpDamagePercentage
                    }
                };

                enemy.TakeDamage(damageContext);
            }


            // エフェクトのプールができたらInstantiate→Destroyの流れをやめる
            if (effects.Count > 0)
            {
                var prefab = effects[i % effects.Count];
                var effect = Object.Instantiate(prefab, target.Position, Quaternion.identity);
                Object.Destroy(effect, 2f);
            }
        }
    }
}
