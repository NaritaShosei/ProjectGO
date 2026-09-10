using System;

/// <summary>表示対象4種の取得回数と初回取得順を保持する。UIの非表示中も記録する。</summary>
public sealed class StatUpgradeState
{
    public const int StatCount = 4;

    public int AcquiredCount => _acquiredCount;

    public static int GetStatIndex(StatSkillType type)
    {
        // enumには表示対象外のDefenseがあるため、enum値を配列添字にしない。
        return type switch
        {
            StatSkillType.Attack => 0,
            StatSkillType.HP => 1,
            StatSkillType.Thunder => 2,
            StatSkillType.Critical => 3,
            _ => -1
        };
    }

    public int GetAcquisitionCount(int index) => _acquisitionCounts[index];
    public int GetOrderedIndex(int position) => _acquisitionOrder[position];

    public void AddAcquisition(StatSkillType type)
    {
        int index = GetStatIndex(type);
        if (index < 0) return;

        if (_acquisitionCounts[index] == 0)
            _acquisitionOrder[_acquiredCount++] = index;

        // 装飾上限に到達しても実際の取得回数は記録し続ける。
        if (_acquisitionCounts[index] < int.MaxValue)
            _acquisitionCounts[index]++;
    }

    public void ResetCounts()
    {
        Array.Clear(_acquisitionCounts, 0, _acquisitionCounts.Length);
        Array.Clear(_acquisitionOrder, 0, _acquisitionOrder.Length);
        _acquiredCount = 0;
    }

    private readonly int[] _acquisitionCounts = new int[StatCount];
    private readonly int[] _acquisitionOrder = new int[StatCount];
    private int _acquiredCount;
}
