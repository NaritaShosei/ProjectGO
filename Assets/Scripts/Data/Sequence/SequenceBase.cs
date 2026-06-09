using UnityEngine;

public abstract class SequenceBase : ScriptableObject
{
    public SequenceType SequenceType => _sequenceType;

    /// <summary>
    /// シークエンスが完了したかチェック
    /// </summary>
    public abstract bool IsComplete(SequenceContext context);

    /// <summary>
    /// シークエンス開始時の処理
    /// </summary>
    public abstract void OnSequenceStart(SequenceContext context);

    /// <summary>
    /// シークエンス更新処理
    /// </summary>
    public abstract void OnSequenceUpdate(SequenceContext context);

    [Header("基本設定")]
    [SerializeField] protected SequenceType _sequenceType;
}

public enum SequenceType
{
    Enemy,      // 雑魚敵シークエンス
    Skill,      // スキル獲得シークエンス
    Boss        // ボスシークエンス
}

public class SequenceContext
{
    public SkillManager SkillManager;
    public EnemyManager EnemyManager;
    public ISkillSelectView SkillSelectView;
    public SpawnData CurrentSpawnData;
    public InputHandler InputHandler;
    public IPlayer Player;

    public int SkillSelectCount;

    // 状態
    public int RemainingEnemies;    // 残り敵数
    public int DefeatedCount;       // 撃破数
    public float ElapsedTime;       // 経過時間
    public bool SkillSelected;      // スキル選択済みか
    public bool BossDefeated;       // ボス撃破済みか
}
