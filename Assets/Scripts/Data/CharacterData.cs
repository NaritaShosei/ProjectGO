using UnityEngine;

[CreateAssetMenu(menuName = "GameData/CharacterData", fileName = "CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("キャラクターの名前")]
    [SerializeField] private string _name;
    [Header("キャラクターのパラメータ")]
    [SerializeField] private float _maxHP;
    [SerializeField] private float _maxStamina;
    [SerializeField] private float _moveSpeed;
    public string Name => _name;
    public float MaxHP => _maxHP;
    public float MaxStamina => _maxStamina;
    public float MoveSpeed => _moveSpeed;
}
