using UnityEngine;

public abstract class PhaseData : ScriptableObject
{
    public PhaseType PhaseType => _phaseType;

    /// <summary>
    /// フェーズが完了したかチェック
    /// </summary>
    public abstract bool IsComplete(PhaseContext context);

    /// <summary>
    /// フェーズ開始時の処理
    /// </summary>
    public abstract void OnPhaseStart(PhaseContext context);

    /// <summary>
    /// フェーズ更新処理
    /// </summary>
    public abstract void OnPhaseUpdate(PhaseContext context);

    [Header("基本設定")]
    [SerializeField] protected PhaseType _phaseType;
}

public enum PhaseType
{
    Enemy,      // 雑魚敵フェーズ
    Skill,      // スキル獲得フェーズ
    Boss        // ボスフェーズ
}

public struct PhaseContext
{
    public EnemyManager EnemyManager;
    // public SkillUIManager SkillUIManager;
    public SpawnData CurrentSpawnData;

    // 状態
    public int RemainingEnemies;    // 残り敵数
    public int DefeatedCount;       // 撃破数
    public float ElapsedTime;       // 経過時間
    public bool SkillSelected;      // スキル選択済みか
    public bool BossDefeated;       // ボス撃破済みか
}