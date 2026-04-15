using UnityEngine;

[CreateAssetMenu(fileName = "LightningStrike", menuName = "GameData/Skill/LightningStrike")]
public class LightningStrike : SkillBase, ISkillUpdater
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
    [SerializeField] private float _damegeMultiplier = 1f;
    [SerializeField] private float _searchRadius = 999f;
    [SerializeField] private GameObject _hitEffectPrefab;

    private float _timer;

    private void Trigger(IPlayerStats stats,Vector3 playerPosition, EnemyManager enemyManager)
    {
        var enemies = enemyManager.GetEnemiesInRange(playerPosition,_searchRadius);

        Debug.Log($"敵の数: {enemies.Count}");

        if (enemies.Count == 0f) return;

        var target = enemies[Random.Range(0,enemies.Count)];

        Debug.Log($"ターゲット: {target}");

        float damage = stats.AttackPower * _damegeMultiplier;

        var damageContext = new DamageContext { AttackPower = damage, PlayerMode = PlayerMode.Thunder };

        target.TakeDamage(damageContext);

        //仮のヒットエフェクト
        if(_hitEffectPrefab != null)
        {
            var effect = Instantiate(_hitEffectPrefab, target.Position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        Debug.Log("落雷発動");

        // 視覚確認
        Debug.DrawLine(playerPosition, target.Position, Color.blue, 1f);
    }
}
