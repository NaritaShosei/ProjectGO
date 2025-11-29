using UnityEngine;

public abstract class PlayerMode
{
    protected PlayerManager _manager;
    protected PlayerAttacker _attacker;
    protected PlayerModeData _data;

    public PlayerModeData Data => _data;

    public void Init(PlayerManager manager, PlayerAttacker attacker, PlayerModeData data)
    {
        _manager = manager;
        _attacker = attacker;
        _data = data;
    }

    public virtual void OnEnter()
    {
        Debug.Log($"[Mode] {_data.ModeName} 開始");
    }

    public virtual void OnExit() { }

    public virtual void OnUpdate() { }

    // モード固有の特殊能力
    public virtual void OnSpecialAbility() { }
}