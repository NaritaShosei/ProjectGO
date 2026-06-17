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
    /// 闘神モード中、攻撃中(Attacking)・チャージ/振りかぶり中(Charging)はダメージによる怯みリアクションを無効化する。
    /// </summary>
    public bool CanInterrupt(PlayerState state)
    {
        // 攻撃中・振りかぶり(チャージ)中は怯まない
        return state != PlayerState.Attacking && state != PlayerState.Charging &&
            _modeProvider.CurrentMode == PlayerMode.Warrior;
    }

    private IModeProvider _modeProvider;
}

