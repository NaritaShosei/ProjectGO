/// <summary>
/// インゲームの各フェーズが実装するインターフェース。
/// </summary>
public interface ISequenceState
{
    /// <summary>Stateの種別</summary>
    SequenceStateType StateType { get; }

    /// <summary>State開始時に一度だけ呼ばれる</summary>
    void OnEnter(SequenceStateContext context);

    /// <summary>毎フレーム呼ばれる。次のStateを返す場合は遷移先のTypeを返し、継続なら null を返す</summary>
    SequenceStateType? Tick(SequenceStateContext context, float deltaTime);

    /// <summary>State終了時に一度だけ呼ばれる</summary>
    void OnExit(SequenceStateContext context);
}

public enum SequenceStateType
{
    IntroMovie,         // 導入ムービー
    MobAndSkill,        // モブ戦 + スキル選択（繰り返し）
    BossIntroMovie,     // ボス登場ムービー
    BossBattle,         // ボス戦
    EndingMovie,        // エンディングムービー
    Result,             // リザルト
    GameOver,           // ゲームオーバー
}
