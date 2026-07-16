using System;
using UnityEngine;

[Serializable]
public class AttackVariantData
{
    public string AttackName => _attackName;
    public ChargeLevel RequiredCharge => _requiredCharge;
    public AttackHitData GetHitData(int hitIndex)
    {
        if (_hits == null || _hits.Length == 0)
        {
            Debug.LogWarning($"{_attackName} has no hit data. Using default hit data.");
            return AttackHitData.Default;
        }

        return _hits[Mathf.Clamp(hitIndex, 0, _hits.Length - 1)] ?? AttackHitData.Default;
    }

    public bool EnableHoming => _enableHoming;
    public float HomingRadius => _homingRadius;
    public float HomingAngle => _homingAngle;
    public float HomingStrength => _homingStrength;

    public bool EnableMovement => _enableMovement;
    public AnimationCurve MoveCurve => _moveCurve;
    public float MoveDistance => _moveDistance;
    public float MoveSpeed => _moveSpeed;
    public float MoveDuration => _moveDuration;
    public bool StopOnHit => _stopOnHit;
    public bool IsPhantom => _isPhantom;

    public string AnimationStateName => _animationStateName;
    public float TransitionDuration => _transitionDuration;
    public string ChargeAnimationStateName => _chargeAnimationStateName;
    public float ChargeTransitionDuration => _chargeTransitionDuration;

    public void SetDefaults()
    {
        _hits = new[] { AttackHitData.Default };
        _moveCurve = AnimationCurve.Linear(0, 0, 1, 1);
        _transitionDuration = -1f;
        _chargeTransitionDuration = -1f;
    }

    [Header("攻撃バリアントの基本情報")]
    [SerializeField] private string _attackName;
    [SerializeField] private ChargeLevel _requiredCharge;

    [Header("ヒットデータ")]
    [SerializeField] private AttackHitData[] _hits;

    [Header("ホーミング")]
    [SerializeField] private bool _enableHoming = false;
    [SerializeField] private float _homingRadius = 5f;
    [SerializeField] private float _homingAngle = 45f;
    [SerializeField] private float _homingStrength = 10f;

    [Header("移動")]
    [SerializeField] private bool _enableMovement = false;
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private float _moveDistance = 0f;
    [SerializeField] private float _moveSpeed = 0f;
    [SerializeField] private float _moveDuration = 0f;
    [Tooltip("ヒット時に移動を止めるかどうか")]
    [SerializeField] private bool _stopOnHit = true;
    [Tooltip("すり抜け攻撃かどうか")]
    [SerializeField] private bool _isPhantom = false;

    [Header("アニメーション")]
    [SerializeField] private string _animationStateName;
    [SerializeField] private float _transitionDuration = -1f;
    [SerializeField] private string _chargeAnimationStateName;
    [SerializeField] private float _chargeTransitionDuration = -1f;
}

[Serializable]
public class AttackHitData
{
    public static AttackHitData Default => new()
    {
        DamageMultiplier = 1f,
        AttackRange = 1f,
        AttackRadius = 1f,
        EnableKnockback = false,
        KnockbackPower = 5f,
        KnockbackUpward = 0f,
        PlayGroundHitSE = false,
        AdditionalLightningDamages = Array.Empty<AdditionalLightningDamageData>(),
    };

    public bool HasAdditionalLightningDamage =>
        AdditionalLightningDamages != null && AdditionalLightningDamages.Length > 0;

    [Header("ダメージ")]
    public float DamageMultiplier = 1f;

    [Header("攻撃範囲")]
    public float AttackRange = 1f;
    public float AttackRadius = 1f;

    [Header("ノックバック")]
    public bool EnableKnockback = false;
    public float KnockbackPower = 5f;
    public float KnockbackUpward = 0f;

    [Header("ヒットストップ")]
    public HitStopData HitStopData;

    [Header("サウンド")]
    public bool PlayGroundHitSE = false;

    [Header("雷の追加ダメージ")]
    public AdditionalLightningDamageData[] AdditionalLightningDamages;

    [Header("カメラシェイク")]
    public CameraShakeData CameraShakeData;
}
