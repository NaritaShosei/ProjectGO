using UnityEngine;

[CreateAssetMenu(menuName = "GameData/CharacterData", fileName = "CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("キャラクターの名前")]
    [SerializeField] private string _name = "";
    [Header("キャラクターのパラメータ")]
    [SerializeField] private float _maxHP = 50;
    [SerializeField] private float _maxStamina = 50;
    [SerializeField] private float _moveSpeed = 5;
    [SerializeField] private float _staminaRegenRate = 10;
    [SerializeField] private float _dodgeStamina = 5;
    [SerializeField] private float _dodgeSpeed = 10;
    [SerializeField] private float _dodgeDuration = 0.2f;
    [SerializeField] private float _canDodgeAttackDuration = 0.5f;
    [SerializeField] private float _hitStopDuration = 0.2f;
    [SerializeField] private float _invincibleDuration = 0.2f;
    [Header("モーション設定")]
    [SerializeField] private AnimationClip _smallHitClip;
    [SerializeField] private AnimationClip _bigHitClip;
    [SerializeField] private AnimationClip _dodgeClip;
    public string Name => _name;
    public float MaxHP => _maxHP;
    public float MaxStamina => _maxStamina;
    public float MoveSpeed => _moveSpeed;
    public float StaminaRegenRate => _staminaRegenRate;
    public float DodgeStamina => _dodgeStamina;
    public float DodgeSpeed => _dodgeSpeed;
    public float DodgeDuration => _dodgeDuration;
    public float CanDodgeAttackDuration => _canDodgeAttackDuration;
    public float HitStopDuration => _hitStopDuration;
    public float InvincibleDuration => _invincibleDuration;
    public AnimationClip SmallHitClip => _smallHitClip;
    public AnimationClip BigHitClip => _bigHitClip;
    public AnimationClip DodgeClip => _dodgeClip;
}
