using UnityEngine;
using System;

public interface IModeController
{
    public PlayerMode CurrentMode { get; }
    public event Action<PlayerMode> OnModeChanged;
    public ModeData ModeData { get; }
    public bool CanSwitchMode(PlayerMode mode);
    public void SwitchMode(PlayerMode mode);    
}
