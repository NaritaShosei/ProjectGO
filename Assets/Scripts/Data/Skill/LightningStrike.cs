using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LightningStrike", menuName = "GameData/Skill/LightningStrike")]
public class LightningStrike : SkillBase,ISkillUpdater
{
    public void OnUpdate(float deltaTime, PlayerMode mode, IPlayerStats stats, Vector3 playerPosition, EnemyManager enemyManager)
    {
        if (mode != PlayerMode.Thunder) return;

        _timer -= deltaTime;

        if (_timer > 0f) return;

        _timer = Random.Range(_minInterval, _maxInterval);

        Trigger(stats, playerPosition, enemyManager);
    }

    [SerializeField] private int _targetCount = 1;
    [SerializeField] private float _minInterval = 2f;
    [SerializeField] private float _maxInterval = 4f;
    [SerializeField] private float _damageMultiplier = 1.6f;
    [SerializeField] private float _searchRadius = 999f;
    //エフェクト
    [SerializeField] private GameObject[] _hitEffectPrefabs;
    //範囲
    [SerializeField] private float _areaRadius;
    //感電時間
    [SerializeField] private float _electricShockDuration;

    [System.NonSerialized] private float _timer;

    private void Trigger(IPlayerStats stats, Vector3 playerPosition, EnemyManager enemyManager)
    {
        var enemies = enemyManager.GetEnemiesInRange(playerPosition, _searchRadius);

        if (enemies.Count == 0) return;

        var shuffled = new List<IEnemy>(enemies);
        Shuffle(shuffled);

        int targetCount = Mathf.Min(_targetCount, shuffled.Count);

        var effects = _hitEffectPrefabs != null ? new List<GameObject>(_hitEffectPrefabs) : new List<GameObject>();
        Shuffle(effects);

        for (int i = 0; i < targetCount; i++)
        {
            var target = shuffled[i];

            var areaTargets = enemyManager.GetEnemiesInRange(target.Position, _areaRadius);

            foreach (var enemy in areaTargets)
            {
                float damage = stats.AttackPower * _damageMultiplier;

                var damageContext = new DamageContext { AttackPower = damage, PlayerMode = PlayerMode.Thunder };
                enemy.TakeDamage(damageContext);

                enemy.ConditionController?.ApplyCondition(new ElectrifiedCondition(_electricShockDuration, enemyIsBoss: enemy.IsBoss));
            }

            GameObject effectPrefab = null;
            if (effects.Count > 0)
            {
                effectPrefab = effects[i % effects.Count];
            }

            //仮のヒットエフェクト
            if (effectPrefab != null)
            {
                var effect = Instantiate(effectPrefab, target.Position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            // 視覚確認
            Debug.DrawLine(playerPosition, target.Position, Color.blue, 1f);
        }
    }

    private void OnEnable()
    {
        _timer = Random.Range(_minInterval, _maxInterval);
    }

    /// <summary>
    /// エフェクトシャッフル
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}
