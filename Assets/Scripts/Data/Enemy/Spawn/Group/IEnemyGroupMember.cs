/// <summary>
/// 敵グループメンバーを表すインターフェース
/// </summary>
public interface IEnemyGroupMember
{
    EnemyGroup Group { get; }
    bool IsGroupLeader { get; }

    /// <summary>
    /// 敵グループに所属させる
    /// </summary>
    /// <param name="group"></param>
    /// <param name="isLeader">リーダーかどうか</param>
    void AssignGroup(
        EnemyGroup group,
        bool isLeader);

    /// <summary>
    /// 敵グループから脱退させる
    /// </summary>
    void ClearGroup();

    /// <summary>
    /// グループリーダーかどうかを設定する
    /// </summary>
    /// <param name="isLeader"></param>
    void SetGroupLeader(bool isLeader);
}
