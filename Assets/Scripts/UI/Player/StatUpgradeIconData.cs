using System;
using UnityEngine;

/// <summary>各ステータスの強化レベル（取得回数）と、その時点で開放する素材の組。</summary>
[Serializable]
public struct StatUpgradeIconData
{
    public int UnlockLevel => _unlockLevel;
    public Sprite IconSprite => _sprite;

    public StatUpgradeIconData(int unlockLevel, Sprite sprite)
    {
        _unlockLevel = unlockLevel;
        _sprite = sprite;
    }

    /// <summary>到達済みの最大開放レベルを選ぶ。未開放なら-1、同レベルなら先頭を採用する。</summary>
    public static int GetUnlockedIndex(int level, StatUpgradeIconData[] entries)
    {
        int selectedIndex = -1;
        int selectedLevel = 0;
        if (entries == null) return selectedIndex;

        // 配列順ではなく条件を比較するため、不規則な間隔や並び順でも素材と条件がずれない。
        for (int i = 0; i < entries.Length; i++)
        {
            int unlockLevel = entries[i].UnlockLevel;
            if (unlockLevel <= selectedLevel || unlockLevel > level) continue;
            selectedIndex = i;
            selectedLevel = unlockLevel;
        }
        return selectedIndex;
    }

    [Tooltip("このステータスの取得回数。プレイヤー全体のレベルではありません。")]
    [Min(1)] [SerializeField] private int _unlockLevel;
    [SerializeField] private Sprite _sprite;
}
