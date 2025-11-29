using UnityEngine;

[CreateAssetMenu(menuName = "GameData/ThunderGodData", fileName = "ThunderGodData")]
public class ThunderGodData : AbilityDataBase
{
    [SerializeField] private float _staminaDecreasePerSecond = 5;

    public float StaminaDecreasePerSecond => _staminaDecreasePerSecond;
}
