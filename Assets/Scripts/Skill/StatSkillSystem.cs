using System;
using UnityEngine;

/// <summary>
/// EXPManager の閾値通知を受けてパラメーターを自動強化する。
/// 選択UIは出さず即時適用。
/// </summary>
public class StatSkillSystem
{
    private const int MAX_ACQUIRE_COUNT = 2;

    public event Action<StatSkillType> OnApply;

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
        _expManager = expManager;

        _expManager.OnLevelUp += AcquireRandom;
    }

    public void Dispose()
    {
        if (_expManager != null)
            _expManager.OnLevelUp -= AcquireRandom;
    }

    private StatSkillData[] _statSkillDataArray;
    private readonly IPlayerStats _stats;
    private readonly EXPManager _expManager;

    /// <summary>
    /// レベルアップのたびに、_statSkillDataArray からランダムに1つ選んでパラメーターを増加させる。
    /// 上昇量は、選ばれたスキルの CalculateAmount() を呼び出して決定する。
    /// </summary>
    /// <summary>
    /// レベルアップのたびに、_statSkillDataArray からランダムに2つ選んでパラメーターを増加させる。
    /// 同じスキルは重複して選ばれない。
    /// </summary>
    private void AcquireRandom(int level)
    {
        if (_statSkillDataArray == null || _statSkillDataArray.Length == 0)
            return;

        // 取得数は最大2個
        // データが1個しかない場合は1個だけ取得
        int acquireCount = Mathf.Min(MAX_ACQUIRE_COUNT, _statSkillDataArray.Length);

        // 重複なし抽選用のインデックス配列
        int[] indices = new int[_statSkillDataArray.Length];

        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = i;
        }

        // 必要な数だけシャッフル
        for (int i = 0; i < acquireCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, indices.Length);

            (indices[i], indices[randomIndex]) =
                (indices[randomIndex], indices[i]);
        }

        // 選ばれたスキルを適用
        for (int i = 0; i < acquireCount; i++)
        {
            var data = _statSkillDataArray[indices[i]];

            if (data == null)
            {
                Debug.LogWarning(
                    "[StatSkill] StatSkillData に null 要素があります。設定を確認してください。");

                continue;
            }

            float baseValue = GetBaseValue(data.StatType);
            float amount = data.CalculateAmount(baseValue);

            Apply(data.StatType, amount);

            #region Debug

            float value = data.StatType switch
            {
                StatSkillType.HP => _stats.MaxHealth,
                StatSkillType.Attack => _stats.AttackPower,
                StatSkillType.Critical => _stats.CriticalRate,
                StatSkillType.Thunder => _stats.MaxThunderGauge,
                _ => 0f
            };

            Debug.Log(
                $"[StatSkill] {data.DisplayName} +{amount:F3} = {value} 自動取得");

            #endregion
        }
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
            case StatSkillType.HP: _stats.AddModifier(new DefaultModifier(amount, StatType.Health)); break;
            case StatSkillType.Attack: _stats.AddModifier(new DefaultModifier(amount, StatType.Attack)); break;
            case StatSkillType.Critical: _stats.AddModifier(new DefaultModifier(amount, StatType.CriticalRate)); break;
            case StatSkillType.Thunder: _stats.AddModifier(new DefaultModifier(amount, StatType.ThunderGauge)); break;
        }

        OnApply?.Invoke(type);
    }
}

public class DefaultModifier : IStatModifier
{
    public DefaultModifier(float amount, StatType type)
    {
        _amount = amount;
        _targetStat = type;
    }

    public StatType TargetStat => _targetStat;

    public virtual float Modify(float baseValue)
    {
        return baseValue + _amount;
    }

    private float _amount;
    private StatType _targetStat;
}
