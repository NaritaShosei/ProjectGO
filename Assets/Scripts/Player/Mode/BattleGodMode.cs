using UnityEngine;

public class BattleGodMode : PlayerMode
{
    public override void OnEnter()
    {
        base.OnEnter();
        _manager.ModeChange(PlayerModeType.Battle);
    }
}
