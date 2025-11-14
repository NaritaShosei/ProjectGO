using UnityEngine;

[CreateAssetMenu(menuName = "GameData/AttackData", fileName = "AttackData")]
public class AttackData : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField] private string _attackName;
    [SerializeField] private float _power;
    [SerializeField] private float _range;
    [SerializeField] private float _speed;
    [SerializeField, Tooltip("ノックバックの強さ")] private float _knockbackForce;
    [SerializeField, Tooltip("ノックバックの向き")] private Vector3 _knockbackDirection;

    [Header("モーション設定")]
    [SerializeField] private AnimationClip _animationClip;
    [SerializeField] private AudioClip _soundEffect;
    [SerializeField] private float _motionDuration;

    [Header("次の攻撃（コンボ遷移）")]
    [SerializeField] private AttackData _nextCombo;

    // ===== プロパティ =====
    public string AttackName => _attackName;
    public float Power => _power;
    public float Range => _range;
    public float Speed => _speed;
    /// <summary>
    /// ノックバックの強さ
    /// </summary>
    public float KnockbackForce => _knockbackForce;
    /// <summary>
    /// ノックバックの向き
    /// </summary>
    public Vector3 KnockbackDirection => _knockbackDirection;

    public AnimationClip AnimationClip => _animationClip;
    public AudioClip SoundEffect => _soundEffect;
    public float MotionDuration => _motionDuration;

    public AttackData NextCombo => _nextCombo;
}
