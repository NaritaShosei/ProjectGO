using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModeController : MonoBehaviour, IModeController
{
    public PlayerMode CurrentMode => _currentMode;
    public ModeData ModeData => GetCurrentModeData();

    public event Action<PlayerMode> OnModeChanged;

    /// <summary>
    /// Player.Init から PlayerStats を渡してモード切替ガードを有効化する。
    /// </summary>
    public void Init(PlayerStats playerStats)
    {
        _playerStats = playerStats;
    }

    /// <summary>
    /// 状態を変更せずに切替可能かを判定する。
    /// 雷神モードへの切替は使用可能なゲージが残っている場合のみ許可する。
    /// </summary>
    public bool CanSwitchMode(PlayerMode newMode)
    {
        return _currentMode != newMode
            && (newMode != PlayerMode.Thunder
                || _playerStats == null
                || _playerStats.CanUseThunder);
    }

    /// <summary>切替条件を再確認し、成立した場合だけモード変更を通知する。</summary>
    public void SwitchMode(PlayerMode newMode)
    {
        if (!CanSwitchMode(newMode)) return;

        _currentMode = newMode;
        OnModeChanged?.Invoke(newMode);
    }

    [SerializeField] private ModeData _warriorData;
    [SerializeField] private ModeData _thunderData;

    private PlayerMode _currentMode;
    private PlayerStats _playerStats;

    private readonly Dictionary<PlayerMode, ModeData> _players = new();

    private void Awake()
    {
        InitializeModeTable();
    }

    private void InitializeModeTable()
    {
        _players.Clear();
        if (_warriorData != null) _players.Add(PlayerMode.Warrior, _warriorData);
        if (_thunderData != null) _players.Add(PlayerMode.Thunder, _thunderData);
    }

    private ModeData GetCurrentModeData()
    {
        _players.TryGetValue(_currentMode, out var data);
        return data;
    }
}
