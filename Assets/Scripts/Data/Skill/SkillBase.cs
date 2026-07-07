using UnityEngine;

public class SkillBase : ScriptableObject, ISkill
{
    public int ID => _id;
    public int Priority => _priority;
    public string Name => _name;
    public string Explanation => _explanation;
    public Sprite Icon => _icon;
    public SkillTiming Timing => _timing;
    public SkillBase UnlockedSkill => _unlockedSkill;
    public bool DefaultUnlocked => _defaultUnlocked;
    public bool CanReselection => _canReselection;

    /// <summary>
    /// 攻撃時スキルが実装する。
    /// </summary>
    public virtual void Apply(ref AttackContext context) { }

    /// <summary>
    /// 攻撃時スキルが実装する。
    /// </summary>
    public virtual bool CanApply(AttackContext context, AttackData data) => true;

    /// <summary>
    /// 獲得時スキルが実装する。
    /// </summary>
    public virtual void OnAcquire(IPlayerStats stats) { }

    /// <summary>
    /// Timing == Passive のスキルが実装する。
    /// SkillManager が自動で呼ぶので、Passive 以外では override 不要。
    /// </summary>
    public virtual ISkillUpdater CreateUpdater() => null;

    [Header("基本情報")]
    [Tooltip("スキルの識別ID。ユニークである必要がある。")]
    [SerializeField] private int _id;               // 検索用ID
    [SerializeField] private int _priority;         // スキル発動順優先度
    [Tooltip("スキルの名前。")]
    [SerializeField] private string _name;          // スキルの名前
    [Tooltip("スキルの説明。")]
    [SerializeField] private string _explanation;   // スキルの説明
    [Tooltip("スキルのアイコン画像。")]
    [SerializeField] private Sprite _icon;          // スキルのアイコン画像
    [Tooltip("スキルの発動タイミング。")]
    [SerializeField] private SkillTiming _timing = SkillTiming.OnAttack; // スキルの発動タイミング
    [Header("開放")]
    [Tooltip("このスキルを獲得した際に開放されるスキル。")]
    [SerializeField] private SkillBase _unlockedSkill;
    [Tooltip("ゲーム開始時にこのスキルがアンロックされているかどうか。")]
    [SerializeField] private bool _defaultUnlocked;
    [Tooltip("このスキルを再選択できるようにするか")]
    [SerializeField] private bool _canReselection;
}
