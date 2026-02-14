using UnityEngine;
public interface IPlayer :
    ICharacter, IPlayerStats
{
}

public interface IPlayerStats :
    IHealth,
    IAttackStats,
    IDefenseStats,
    IHealthStats,
    IStaminaStats,
    IStatUpgradable
{

}
