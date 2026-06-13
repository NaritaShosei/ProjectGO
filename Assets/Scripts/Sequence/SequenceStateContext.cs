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
    public InputHandler InputHandler;
    public IPlayer Player;
    public SequenceManager SequenceManager;

    #endregion

    #region タイマー

    /// <summary>モブ戦・ボス戦などの制限時間管理</summary>
    public CountDownTimer PhaseTimer;

    /// <summary>ゲームオーバー選択タイマー（10秒）</summary>
    public CountDownTimer GameOverTimer;

    /// <summary>スキル選択タイマー</summary>
    public CountDownTimer SkillSelectTimer;

    public float MobBattleTimeLimit;
    public float BossBattleTimeLimit;
    public float SkillSelectTimeLimit;
    #endregion

    #region フラグ

    /// <summary>プレイヤーが死亡したか</summary>
    public bool IsPlayerDead;

    /// <summary>制限時間が切れたか</summary>
    public bool IsTimeUp;

    /// <summary>ボスが撃破されたか</summary>
    public bool IsBossDefeated;

    /// <summary>ムービーが完了したか（仮実装用フラグ）</summary>
    public bool IsMovieCompleted;

    /// <summary>ゲームオーバー後にリスタートが選ばれたか</summary>
    public bool IsRestartRequested;

    /// <summary>ゲームオーバー後にタイトルへ戻るが選ばれたか</summary>
    public bool IsTitleRequested;

    #endregion

    #region スキル選択

    /// <summary>スキル選択が完了したか</summary>
    public bool IsSkillSelected;

    /// <summary>スキル選択候補の数</summary>
    public int SkillSelectCount = 3;

    public ISkillSelectView SkillSelectView;

    #endregion

    #region Spawn

    /// <summary>WaveSystemで使用するWaveSequenceData</summary>
    public WaveSequenceData WaveSequenceData;

    /// <summary>SpawnPointSelector（WaveControllerに渡す）</summary>
    public SpawnPointSelector SpawnPointSelector;

    /// <summary>ボス用SpawnData</summary>
    public SpawnData BossSpawnData;

    #endregion

    #region リザルト
    public readonly struct ResultData
    {
        public readonly int Kills;
        public readonly int Level;
        public readonly float ClearTime;
        public ResultData(int kills, int level, float clearTime)
        {
            Kills = kills;
            Level = level;
            ClearTime = clearTime;
        }
    }

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
