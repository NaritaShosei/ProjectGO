using UnityEngine;

public class BattleGodMode : PlayerMode
{
    public override void OnEnter()
    {
        _manager.ModeChange(PlayerModeType.Battle);
    }
}
