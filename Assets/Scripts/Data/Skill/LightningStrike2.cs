using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LightningStrike2", menuName = "GameData/Skill/LightningStrike2")]
public class LightningStrike2 : SkillBase,ISkillUpdater
{
    public void OnUpdate(float deltaTime, PlayerMode mode, IPlayerStats stats, Vector3 playerPosition, EnemyManager enemyManager)
    {
        if (mode != PlayerMode.Thunder) return;

        _timer -= deltaTime;

        if (_timer > 0f) return;

        _timer = Random.Range(_minInterval, _maxInterval);

        Trigger(stats, playerPosition, enemyManager);

        Debug.Log("落雷発動");
    }

    [SerializeField] private float _minInterval = 2f;
    [SerializeField] private float _maxInterval = 4f;
    [SerializeField] private float _damegeMultiplier = 1.3f;
    [SerializeField] private float _searchRadius = 999f;
    [SerializeField] private GameObject[] _hitEffectPrefabs;

    private float _timer;

    private void Trigger(IPlayerStats stats, Vector3 playerPosition, EnemyManager enemyManager)
    {
        var enemies = enemyManager.GetEnemiesInRange(playerPosition, _searchRadius);

        Debug.Log($"敵の数: {enemies.Count}");

        if (enemies.Count == 0f) return;

        var targets = new List<IEnemy>();
        for (int i = 0;  i < 2 ; i++)
        {
            var target = enemies[Random.Range(0, enemies.Count)];
            targets.Add(target);
        }

        var effects = new List<GameObject>(_hitEffectPrefabs);
        Shuffle(effects);

        for (int i = 0; i < 2; i++)
        {
            var target = targets[i];
            var effectPrefab = effects[i % effects.Count];

            float damage = stats.AttackPower * _damegeMultiplier;

            var damageContext = new DamageContext { AttackPower = damage, PlayerMode = PlayerMode.Thunder };

            target.TakeDamage(damageContext);

            //仮のヒットエフェクト
            if (effectPrefab != null)
            {
                var effect = Instantiate(effectPrefab, target.Position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            // 視覚確認
            Debug.DrawLine(playerPosition, target.Position, Color.blue, 1f);

            Debug.Log("落雷発動");
        }
    }

    //エフェクトシャッフル
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}
