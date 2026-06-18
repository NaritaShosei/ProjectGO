using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MoveData", menuName = "GameData/MoveData")]
public class MoveData : ScriptableObject
{
    public float RotateSpeed => _rotateSpeed;

    /// <summary>モードごとの回避パラメータ</summary>
    public DodgeData GetDodge(PlayerMode mode)
    {
        if (_modeDodgeData != null)
        {
            foreach (var entry in _modeDodgeData)
            {
                if (entry.Mode == mode) return entry.Data;
            }
        }
        // フォールバック
        return _defaultDodge;
    }

    [SerializeField] private float _rotateSpeed = 5;

    [Tooltip("モードごとの回避設定")]
    [SerializeField] private List<ModeDodgeEntry> _modeDodgeData;

    [Tooltip("モード設定がない場合のフォールバック")]
    [SerializeField] private DodgeData _defaultDodge;
}

[Serializable]
public struct ModeDodgeEntry
{
    public PlayerMode Mode;
    public DodgeData Data;
}

[Serializable]
public struct DodgeData
{
    public float Speed;
    public float Duration;
    [Tooltip("回避の無敵時間")]
    public float InvincibleDuration;
}
