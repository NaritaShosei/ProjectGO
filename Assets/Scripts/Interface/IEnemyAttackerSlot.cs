using System;

/// <summary>
/// 同時攻撃可能数を管理するスロットのインターフェース
/// </summary>
public interface IEnemyAttackerSlot
{
    /// <summary>
    /// 指定したEnemyがスロットを確保済みかどうかを返す
    /// MoveBehaviourのCanEnterで使用する
    /// </summary>
    bool IsAcquired(int enemyId);

    /// <summary>
    /// スロットを確保する
    /// すでに確保済みの場合はtrueを返す
    /// Boss以外はスロット上限を超えた場合はfalseを返す
    /// </summary>
    bool TryAcquire(int enemyId, int slotCost, bool isBoss);

    /// <summary>
    /// スロットを解放する
    /// </summary>
    void Release(int enemyId, int slotCost);

    /// <summary>
    /// スロットをリセットする
    /// シーン再ロードやゲームリセット時に全スロット状態をクリアする想定
    /// </summary>
    void Reset();

    /// <summary>
    /// スロットが解放されたときに発火するイベント
    /// 未取得の敵がこのイベントを受けてTryAcquireを再試行する
    /// </summary>
    event Action OnSlotReleased;
}
