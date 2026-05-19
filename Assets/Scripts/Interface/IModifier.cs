public interface IStatModifier
{
    StatType TargetStat { get; }

    float Modify(float current);
}

public enum StatType
{
    Health,
    Attack,
    Defense,
    CriticalRate,
    ThunderDrain,
    ThunderRecover,
    ThunderGauge,
    Heal,
    DodgeInvincibleTime
}

/// <summary>
/// プレイヤーがダメージを受ける際のリアクションを有効かどうかを判断するインターフェース
/// </summary>
public interface IDamageReactionModifier
{
    /// <summary>
    /// Stateに応じてダメージリアクションを有効にするかどうかを判断する。
    /// </summary>
    bool CanInterrupt(PlayerState state);
}

/// <summary>
/// プレイヤーがダメージを受ける際のダメージ量を修正するインターフェース。
/// </summary>
public interface IDamageModifier
{
    void Modify(ref float damage, PlayerMode mode);
}

/// <summary>
/// プレイヤーの攻撃コンボチェーンを変更するインターフェース。
/// </summary>
public interface IComboModifier
{
    /// <summary>
    /// AttackDataRepositoryに対してコンボチェーンを変更する
    /// </summary>
    void ModifyCombo(AttackDataRepository repository);
}
