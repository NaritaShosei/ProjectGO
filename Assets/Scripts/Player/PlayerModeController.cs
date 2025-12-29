using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModeController : MonoBehaviour, IModeController
{
    public PlayerMode CurrentMode => _currentMode;
    public ModeData ModeData => GetCurrentModeData();

    public event Action<PlayerMode> OnModeChanged;

    public void SwitchMode(PlayerMode newMode)
    {
        if (_currentMode == newMode)
        {
            return;
        }

        _currentMode = newMode;
        OnModeChanged?.Invoke(newMode);
    }

    public ModeData GetCurrentModeData()
    {
        if (_players.TryGetValue(_currentMode, out var data))
        {
            return data;
        }

        return null;
    }

    [SerializeField] private ModeData _warriorData;
    [SerializeField] private ModeData _thunderData;

    private PlayerMode _currentMode;

    private Dictionary<PlayerMode, ModeData> _players = new Dictionary<PlayerMode, ModeData>();

    private void Awake()
    {
        InitializeModeTable();
    }

    private void InitializeModeTable()
    {
        _players.Clear();

        if (_warriorData != null)
        {
            _players.Add(PlayerMode.Warrior, _warriorData);
        }

        if (_thunderData != null)
        {
            _players.Add(PlayerMode.Thunder, _thunderData);
        }
    }
}
