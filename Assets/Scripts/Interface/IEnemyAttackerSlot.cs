/// <summary>
/// 同時攻撃可能数を管理するスロットのインターフェース
/// </summary>
public interface IEnemyAttackerSlot
{
    /// <summary>
    /// スロットの確保を試みる
    /// </summary>
    /// <param name="enemyId">EnemyのInstanceID</param>
    /// <param name="slotCost">消費スロット数</param>
    /// <param name="isBoss">Boss優先確保フラグ</param>
    bool TryAcquire(int enemyId, int slotCost, bool isBoss);

    /// <summary>
    /// スロットを解放する
    /// </summary>
    /// <param name="enemyId">EnemyのInstanceID</param>
    /// <param name="slotCost">解放するスロット数</param>
    void Release(int enemyId, int slotCost);

    /// <summary>
    /// 全スロットをリセットする
    /// </summary>
    void Reset();
}
