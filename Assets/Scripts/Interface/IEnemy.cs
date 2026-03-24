using System;
using UnityEngine;

/// <summary>
/// Enemyの共通インターフェース
/// </summary>
public interface IEnemy : ICharacter
{
    // --- Events ---

    /// <summary>HP変化時に発火するイベント（current, max）</summary>
    event Action<float, float> OnHealthChanged;

    /// <summary>ダメージを受けた際にポップアップ情報を通知するイベント</summary>
    event Action<DamagePopupViewModel> OnDamageDealt;

    /// <summary>ダメージを受けて生存したときに発火するイベント（被弾入れ替え判定に使用）</summary>
    event Action<IEnemy> OnDamaged;

    /// <summary>死亡時に発火するイベント</summary>
    event Action<IEnemy> OnDead;

    // --- Properties ---

    /// <summary>ConditionController への参照</summary>
    IEnemyConditionController ConditionController { get; }

    /// <summary>EnemyAnimator への参照</summary>
    IEnemyAnimator EnemyAnimator { get; }

    /// <summary>ワールド座標</summary>
    Vector3 Position { get; }

    /// <summary>インスタンス識別ID（AttackerSlotのキーに使用）</summary>
    int Id { get; }

    /// <summary>ボス判定</summary>
    bool IsBoss { get; }

    /// <summary>HitStop等で使用するタイムスケール（DeadCondition の物理スケーリングに使用）</summary>
    float TimeScale { get; }

    // --- Methods ---

    /// <summary>Playerの参照を渡して初期化する</summary>
    void Init(IPlayer player);

    /// <summary>攻撃の内容を渡して内部でダメージ計算をする</summary>
    void TakeDamage(DamageContext context);

    /// <summary>ノックバックの力を与える</summary>
    void AddKnockbackForce(Vector3 direction);

    /// <summary>ConditionによりActionを阻害する</summary>
    void OnConditionInterrupt();

    /// <summary>位置を直接セットする</summary>
    void SetPosition(Vector3 position);

    /// <summary>各サービスを注入する。EnemyManagerのSpawnから呼ぶ想定</summary>
    void InjectServices(EnemyServices services);
}

/// <summary>
/// EnemyManagerからEnemyへ注入するサービス群
/// </summary>
public readonly struct EnemyServices
{
    public readonly ISpatialHashGrid SpatialHashGrid;
    public readonly ISeparationService SeparationService;
    public readonly IWallAvoidanceService WallAvoidanceService;
    public readonly IEnemyAttackerSlot AttackerSlot;

    public EnemyServices(
        ISpatialHashGrid spatialHashGrid,
        ISeparationService separationService,
        IWallAvoidanceService wallAvoidanceService,
        IEnemyAttackerSlot attackerSlot)
    {
        SpatialHashGrid = spatialHashGrid;
        SeparationService = separationService;
        WallAvoidanceService = wallAvoidanceService;
        AttackerSlot = attackerSlot;
    }
}
