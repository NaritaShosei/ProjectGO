using UnityEngine;

[CreateAssetMenu(fileName = "CircleSpawnData", menuName = "GameData/SpawnData/Circle")]

public class CircleSpawnData : SpawnData
{
    public enum SpawnEnemyType
    {
        Mob,
        Boss
    }

    public enum SpawnMode
    {
        Individual,
        Group
    }

    public Vector3 Center => _center;
    public float Radius => _radius;
    public SpawnEnemyType SpawnEnemy => _spawnEnemyType;
    public SpawnMode Mode => _spawnMode;
    public float GroupMoveRadius => _groupMoveRadius;

    public override ISpawnStrategy CreateStrategy(EnemyManager enemyManager)
    {
        // 円形生成の ISpawnStrategy
        return new CircleSpawnStrategy(enemyManager, this);
    }

    [Header("生成を行う際の座標情報")]
    [SerializeField] private Vector3 _center;
    [SerializeField] private float _radius;

    [Header("生成するEnemyの種類")]
    [SerializeField, Tooltip("生成するEnemyの種類")] private SpawnEnemyType _spawnEnemyType;

    [Header("生成方式")]
    [SerializeField]
    private SpawnMode _spawnMode =
    SpawnMode.Individual;

    [Header("グループ設定")]
    [SerializeField, Min(0f)]
    private float _groupMoveRadius = 3f;
}

public struct CircleSpawnStrategy : ISpawnStrategy
{
    private readonly EnemyManager _enemyManager;
    private readonly CircleSpawnData _spawnData;

    public CircleSpawnStrategy(
        EnemyManager enemyManager,
        CircleSpawnData spawnData)
    {
        _enemyManager = enemyManager;
        _spawnData = spawnData;
    }

    public void Spawn()
    {
        if (_spawnData == null)
        {
            Debug.LogWarning(
                $"{nameof(CircleSpawnStrategy)}: SpawnDataがnullです。");
            return;
        }

        if (_spawnData.Enemies == null ||
            _spawnData.Enemies.Length == 0)
        {
            Debug.LogWarning(
                $"{nameof(CircleSpawnStrategy)}: Enemyが設定されていません。");
            return;
        }

        switch (_spawnData.Mode)
        {
            case CircleSpawnData.SpawnMode.Individual:
                SpawnIndividuals();
                break;

            case CircleSpawnData.SpawnMode.Group:
                SpawnGroup();
                break;

            default:
                Debug.LogWarning(
                    $"未対応の生成方式です: {_spawnData.Mode}");
                break;
        }
    }

    /// <summary>
    /// 敵をそれぞれ独立した個体として生成する。
    /// </summary>
    private void SpawnIndividuals()
    {
        for (int i = 0;
             i < _spawnData.Enemies.Length;
             i++)
        {
            Vector3 position =
                CalculateCirclePosition(
                    i,
                    _spawnData.Enemies.Length);

            SpawnEnemy(
                _spawnData.Enemies[i],
                position);
        }
    }

    /// <summary>
    /// 敵を同じグループとして生成する。
    /// </summary>
    private void SpawnGroup()
    {
        if (_spawnData.SpawnEnemy !=
            CircleSpawnData.SpawnEnemyType.Mob)
        {
            Debug.LogWarning(
                "グループ生成はMobのみ対応しています。");
            return;
        }

        int count = _spawnData.Enemies.Length;
        var enemyGroup = new EnemyGroup(
            _spawnData.GroupMoveRadius);

        for (int i = 0; i < count; i++)
        {
            bool isLeader = i == 0;

            Vector3 position = isLeader
                ? _spawnData.Center
                : CalculateGroupMemberPosition(
                    i - 1,
                    count - 1);

            Enemy spawnedEnemy = _enemyManager.Spawn(
                _spawnData.Enemies[i],
                position);

            if (spawnedEnemy is IEnemyGroupMember groupMember)
            {
                bool isActualLeader =
                    enemyGroup.Leader == null;

                enemyGroup.AddMember(
                    spawnedEnemy,
                    groupMember,
                    isActualLeader);
            }
        }

        if (enemyGroup.Members.Count > 0)
            _enemyManager.RegisterWaitingGroup(enemyGroup);
    }

    /// <summary>
    /// リーダー周囲のメンバー生成位置を計算する。
    /// </summary>
    private Vector3 CalculateGroupMemberPosition(
        int index,
        int memberCount)
    {
        if (memberCount <= 0)
            return _spawnData.Center;

        return CalculatePositionOnCircle(
            index,
            memberCount,
            _spawnData.GroupMoveRadius);
    }

    /// <summary>
    /// 円周上の生成位置を計算する。
    /// </summary>
    private Vector3 CalculateCirclePosition(
        int index,
        int count)
    {
        return CalculatePositionOnCircle(
            index,
            count,
            _spawnData.Radius);
    }

    private Vector3 CalculatePositionOnCircle(
        int index,
        int count,
        float radius)
    {
        if (count <= 0)
            return _spawnData.Center;

        float angle = index * Mathf.PI * 2f / count;

        Vector3 offset = new Vector3(
            Mathf.Cos(angle),
            0f,
            Mathf.Sin(angle)
        ) * Mathf.Max(0f, radius);

        return _spawnData.Center + offset;
    }

    /// <summary>
    /// 設定された敵種別に応じて生成する。
    /// </summary>
    private void SpawnEnemy(
        string enemyKey,
        Vector3 position)
    {
        switch (_spawnData.SpawnEnemy)
        {
            case CircleSpawnData.SpawnEnemyType.Mob:
                _enemyManager.Spawn(
                    enemyKey,
                    position);
                break;

            case CircleSpawnData.SpawnEnemyType.Boss:
                Debug.Log("Boss戦");

                _enemyManager.SpawnBoss(
                    enemyKey,
                    position);
                break;
        }
    }

}
