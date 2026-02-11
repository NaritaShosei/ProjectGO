using UnityEngine;

/// <summary>
/// アーマーの性能データを設定するデータクラス
/// TODO: Armor用のPrefabを登録して呼び出したかった。。
/// TODO: MenuNameの階層をGameData/Enemy/ArmorDataにしたほうがいいかも
/// </summary>
[CreateAssetMenu(fileName = "ArmorData", menuName = "GameData/ArmorData")]
public class ArmorData : ScriptableObject
{
    //TODO: どういったデータが必要かはプランナーに要確認

    public float MaxHP => _maxHP;

    // public GameObject ArmorPrefab => _armor;

    [Header("Status")]
    [SerializeField] private float _maxHP = 20f;

    // [Header("ArmorPrefab")]
    // [SerializeField] private GameObeject _armorPrefab = null;
}
