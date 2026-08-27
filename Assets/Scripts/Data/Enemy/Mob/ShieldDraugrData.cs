using UnityEngine;

[CreateAssetMenu(fileName = "ShieldDraugrData", menuName = "GameData/Enemy/ShieldDraugrData")]
public class ShieldDraugrData : ScriptableObject
{
    public float ShieldDurability => _shieldDurability;
    public float FrontalDotThreshold => _frontalDotThreshold;
    public string ShieldBrokenEffect => _shieldBrokenEffect;

    public string ShieldDamageEffect => _shieldDamageEffect;
    public Vector3 ShieldBrokenEffectScale => _shieldBrokenEffectScale;
    public float FistAttackChance => _fistAttackChance;
    public float FistRerollInterval => _fistRerollInterval;
    public float PostAttackRecoveryDuration => _postAttackRecoveryDuration;
    public EnemyAttackPattern FistAttackPattern => _fistAttackPattern;

    [SerializeField, Tooltip("盾の耐久値")] private float _shieldDurability = 100f;
    [SerializeField, Range(-1f, 1f), Tooltip("盾で受け止める範囲")] private float _frontalDotThreshold = 0.5f; // 前方約60度以内
    [SerializeField, Tooltip("盾破壊時のエフェクト")] private string _shieldBrokenEffect = "shieldBrokenEffect";
    [SerializeField, Tooltip("盾被ダメージエフェクト")] private string _shieldDamageEffect = "shieldDamageEffect";
    [SerializeField, Tooltip("盾破壊時のエフェクトの大きさ")] private Vector3 _shieldBrokenEffectScale;
    [SerializeField, Tooltip("こぶし攻撃の確率"), Range(0f, 1f)] private float _fistAttackChance = 0.1f;
    [SerializeField, Tooltip("こぶし攻撃の抽選間隔（秒）")] private float _fistRerollInterval = 2f;
    [SerializeField, Tooltip("攻撃後硬直の時間")] private float _postAttackRecoveryDuration = 5f;
    [SerializeField] private EnemyAttackPattern _fistAttackPattern;
}
