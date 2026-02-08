/// <summary>
/// エネミーのステータスUIを表示するためのインターフェース
/// </summary>
public interface IEnemyStatusView
{
    /// <summary>
    /// HP表示の更新用
    /// </summary>
    /// <param name="currentHP"></param>
    /// <param name="maxHP"></param>
    void SetHealth(float currentHP, float maxHP);

    /// <summary>
    /// UIを非表示にする
    /// </summary>
    void Hide();
}
