using System;
using UnityEngine;

public class ThunderGodMode : PlayerMode
{
    private ThunderGodData _thunderGodData;
    private event Action OnStaminaAllDecrease;

    public ThunderGodMode(Action onStaminaAllDecrease)
    {
        OnStaminaAllDecrease += onStaminaAllDecrease;
    }

    protected override void OnInit()
    {
        _thunderGodData = _data.AbilityData as ThunderGodData;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        _manager.ModeChange(PlayerModeType.Thunder);
    }

    public override void OnUpdate()
    {
        if (!_manager.TryUseStamina(_thunderGodData.StaminaDecreasePerSecond * Time.deltaTime))
        {
            OnStaminaAllDecrease?.Invoke();
        }
    }
}
