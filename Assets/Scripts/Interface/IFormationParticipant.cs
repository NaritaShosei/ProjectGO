/// <summary>
/// EnemyFormationSystemへの参加資格を表すインターフェース
/// FormationSystemが必要とするデータへのアクセスを提供する
/// </summary>
public interface IFormationParticipant
{
    /// <summary>
    /// TryAcquire / Release で使用するEnemyの識別ID
    /// MonoBehaviour.GetInstanceID()と一致させること
    /// </summary>
    int EnemyId { get; }

    /// <summary>
    /// 前衛選出の優先度を表すコンバットパワー
    /// 値が高いほど前衛に選ばれやすい
    /// </summary>
    float CombatPower { get; }

    /// <summary>
    /// AttackerSlotを取得する際に消費するスロットコスト
    /// </summary>
    int FormationSlotCost { get; }

    /// <summary>
    /// 攻撃クールダウン中かどうか
    /// 背後移動時のスロット譲渡タイミング判定に使用する
    /// </summary>
    bool IsInAttackCooldown { get; }
}
