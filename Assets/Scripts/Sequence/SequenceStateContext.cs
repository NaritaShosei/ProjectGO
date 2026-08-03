using System;

/// <summary>
/// InGameStateMachineの各Stateが参照する共有コンテキスト。
/// SequenceManagerからセットアップされる。
/// </summary>
public class SequenceStateContext
{
    #region 外部参照

    public EnemyManager EnemyManager;
    public SkillManager SkillManager;
    public EXPManager EXPManager;
    public InputHandler InputHandler;
    public IPlayer Player;
    public SequenceManager SequenceManager;
    public MoviePlayer MoviePlayer;

    #endregion

    #region フラグ

    /// <summary>プレイヤーが死亡したか</summary>
    public bool IsPlayerDead;

    /// <summary>制限時間が切れたか</summary>
    public bool IsTimeUp;

    /// <summary>ゲームオーバーになった原因。</summary>
    public GameOverReason GameOverReason;

    /// <summary>ボスが撃破されたか</summary>
    public bool IsBossDefeated;

    /// <summary>ムービーが完了したか（仮実装用フラグ）</summary>
    public bool IsMovieCompleted;

    /// <summary>ゲームオーバー後にリスタートが選ばれたか</summary>
    public bool IsRestartRequested;

    /// <summary>ゲームオーバー後にタイトルへ戻るが選ばれたか</summary>
    public bool IsTitleRequested;

    /// <summary>スキル選択が完了したか</summary>
    public bool IsSkillSelected;

    #endregion

    #region リザルト
    public ResultData Result;

    #endregion

    #region リセット

    /// <summary>フレーム間フラグをリセットする（State遷移後に呼ぶ）</summary>
    public void ResetTransitionFlags()
    {
        IsTimeUp = false;
        IsMovieCompleted = false;
        IsSkillSelected = false;
        IsRestartRequested = false;
        IsTitleRequested = false;
    }

    #endregion
}
