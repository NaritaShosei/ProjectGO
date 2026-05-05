using UnityEngine;

/// <summary>
/// EXPManager の閾値通知を受けてパラメーターを自動強化する。
/// 選択UIは出さず即時適用。
/// </summary>
public class StatSkillSystem
{
    /// <summary>
    /// コンストラクタで、StatSkillData の配列、プレイヤーのステータスインターフェース、EXPManager を受け取る。
    /// </summary>
    /// <param name="statSkillDataArray"></param>
    /// <param name="stats"></param>
    /// <param name="expManager"></param>
    public StatSkillSystem(
        StatSkillData[] statSkillDataArray,
        IPlayerStats stats,
        EXPManager expManager)
    {
        _statSkillDataArray = statSkillDataArray;
        _stats = stats;

        expManager.OnLevelUp += AcquireRandom;
    }

    [SerializeField] private StatSkillData[] _statSkillDataArray;
    private readonly IPlayerStats _stats;

    /// <summary>
    /// レベルアップのたびに、_statSkillDataArray からランダムに1つ選んでパラメーターを増加させる。
    /// 上昇量は、選ばれたスキルの CalculateAmount() を呼び出して決定する。
    /// </summary>
    private void AcquireRandom(int level)
    {
        if (_statSkillDataArray == null || _statSkillDataArray.Length == 0) return;

        var data = _statSkillDataArray[Random.Range(0, _statSkillDataArray.Length)];
        float baseValue = GetBaseValue(data.StatType);
        float amount = data.CalculateAmount(baseValue);

        Apply(data.StatType, amount);

        #region Debug

        float value = data.StatType switch
        {
            StatSkillType.HP => _stats.MaxHealth,
            StatSkillType.Attack => _stats.AttackPower,
            StatSkillType.Defense => _stats.DefensePower,
            StatSkillType.Critical => _stats.CriticalRate,
            StatSkillType.Thunder => _stats.MaxThunderGauge,
            _ => 0f
        };

        Debug.Log($"[StatSkill] {data.DisplayName} +{amount:F3} = {value} 自動取得");
        #endregion
    }

    /// <summary>
    /// スキルの種類に応じて、基礎値を取得する。
    /// </summary>
    private float GetBaseValue(StatSkillType type)
    {
        return type switch
        {
            StatSkillType.HP => _stats.BaseMaxHealth,
            StatSkillType.Attack => _stats.BaseAttackPower,
            StatSkillType.Defense => _stats.BaseDefensePower,
            StatSkillType.Critical => _stats.BaseCriticalRate,
            StatSkillType.Thunder => _stats.BaseMaxThunderGauge,
            _ => 0f
        };
    }

    /// <summary>
    /// スキルの種類に応じて、プレイヤーのステータスを増加させる。
    /// </summary>
    private void Apply(StatSkillType type, float amount)
    {
        switch (type)
        {
            case StatSkillType.HP: _stats.AddMaxHealth(amount); break;
            case StatSkillType.Attack: _stats.AddAttackPower(amount); break;
            case StatSkillType.Defense: _stats.AddDefensePower(amount); break;
            case StatSkillType.Critical: _stats.AddCriticalRate(amount); break;
            case StatSkillType.Thunder: _stats.AddMaxThunderGauge(amount); break;
        }
    }
}
