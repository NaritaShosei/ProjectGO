using UnityEngine;

/// <summary>
/// アーマーの性能データを設定するデータクラス
/// </summary>
[CreateAssetMenu(fileName = "ArmorData", menuName = "GameData/Enemy/Mob/ArmorData")]
public class ArmorData : ScriptableObject
{
    public float MaxHP => _maxHP;

    [Header("Status")]
    [SerializeField] private float _maxHP = 20f;
}
