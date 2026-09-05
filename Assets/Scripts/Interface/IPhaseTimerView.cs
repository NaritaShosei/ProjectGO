/// <summary>
/// フェーズタイマーを表示するUIが実装するインターフェース。
/// バトルタイマー・スキル選択タイマーどちらにも使う。
/// </summary>
public interface IPhaseTimerView
{
    /// <summary>タイマー表示を更新する。current=残り秒数、max=最大秒数</summary>
    void UpdateTimer(float current, float max);

    void ResetTimer();
}
