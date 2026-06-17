using UnityEngine;

/// <summary>
/// 闘神モード中、攻撃中(Attacking)・チャージ/振りかぶり中(Charging)はダメージによる怯みリアクションを無効化するパッシブスキル。
/// 獲得時にIDamageReactionModifierとしてPlayerへ登録される。
/// </summary>
[CreateAssetMenu(fileName = "PoiseSkill", menuName = "GameData/Skill/PoiseSkill")]
public class PoiseSkill : SkillBase, IDamageReactionModifier
{
    public override void OnAcquire(IPlayerStats stats)
    {
        _modeProvider = stats;
        stats.AddDamageReactionModifier(this);
    }

    /// <summary>
    /// falseを返すと、そのリアクション(怯み = PlayerState.Damagedへの遷移)は発生しない。
    /// 闘神モード中、攻撃中(Attacking)・チャージ/振りかぶり中(Charging)のときのみ無効化する。
    /// </summary>
    public bool CanInterrupt(PlayerState state)
    {
        bool isWarriorMode = _modeProvider != null && _modeProvider.CurrentMode == PlayerMode.Warrior;
        bool isActionState = state == PlayerState.Attacking || state == PlayerState.Charging;

        if (isWarriorMode && isActionState)
        {
            return false; // 怯まない
        }

        return true; // 通常通り怯む
    }

    private IModeProvider _modeProvider;
}

