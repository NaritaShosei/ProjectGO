using UnityEngine;

[CreateAssetMenu(fileName = "LightningStrike", menuName = "GameData/Skill/LightningStrike")]
public class LightningStrike : SkillBase, ISkillUpdater
{
    public void OnUpdate(float deltaTime, PlayerMode mode, IPlayerStats stats,Vector3 playerPosition,EnemyManager enemyManager)
    {
        if (mode != PlayerMode.Thunder) return;

        _timer -= deltaTime;

        if (_timer > 0f) return;

        _timer = UnityEngine.Random.Range(_minInterval, _maxInterval);

        Trigger(stats,playerPosition,enemyManager);
    }

    [SerializeField] private float _minInterval = 2f;
    [SerializeField] private float _maxInterval = 4f;
    [SerializeField] private float _damegeMultiplier = 1f;
    [SerializeField] private float _searchRadius = 999f;

    private float _timer;

    private void Trigger(IPlayerStats stats,Vector3 playerPosition, EnemyManager enemyManager)
    {
        var enemies = enemyManager.GetEnemiesInRange(playerPosition,_searchRadius);

        if (enemies.Count == 0f) return;

        var target = enemies[UnityEngine.Random.Range(0,enemies.Count)];

        float damage = stats.AttackPower * _damegeMultiplier;

        var damageContext = new DamageContext { AttackPower = damage, PlayerMode = PlayerMode.Thunder };

        target.TakeDamage(damageContext);
        
        Debug.Log("落雷発動");
    }

    //このスキルのランダムな敵に攻撃は、攻撃範囲があるのか？
    //
}
