using System;
using UnityEngine;

[Serializable]
public class AttackVariantData
{
    /// <summary> 攻撃名 </summary>
    public string AttackName => _attackName;
    /// <summary> 必要な溜めレベル </summary>
    public ChargeLevel RequiredCharge => _requiredCharge;

    /// <summary> 攻撃力倍率 </summary>
    public float DamageMultiplier => _damageMultiplier;

    /// <summary> 攻撃の届く距離 </summary>
    public float AttackRange => _attackRange;
    /// <summary> 攻撃の当たり判定の半径 </summary>
    public float AttackRadius => _attackRadius;

    /// <summary> ノックバックを有効にするかどうか </summary>
    public bool EnableKnockback => _enableKnockback;
    /// <summary> ノックバックの強さ </summary>
    public float KnockbackPower => _knockbackPower;
    /// <summary> ノックバックの垂直成分 </summary>
    public float KnockbackUpward => _knockbackUpward;

    /// <summary> ホーミングを有効にするかどうか </summary>
    public bool EnableHoming => _enableHoming;
    /// <summary> ホーミングの探索半径 </summary>
    public float HomingRadius => _homingRadius;
    /// <summary> ホーミングの探索角度 </summary>
    public float HomingAngle => _homingAngle;
    /// <summary> ホーミングの強さ（大きいほどターゲットに向かって急激に曲がる） </summary>
    public float HomingStrength => _homingStrength;

    /// <summary> 攻撃中の移動を有効にするかどうか </summary>
    public bool EnableMovement => _enableMovement;
    /// <summary> 移動の速度変化を制御するアニメーションカーブ </summary>
    public AnimationCurve MoveCurve => _moveCurve;
    /// <summary> 移動距離 </summary>
    public float MoveDistance => _moveDistance;
    /// <summary> 移動速度 </summary>
    public float MoveSpeed => _moveSpeed;
    /// <summary> 移動時間 </summary>
    public float MoveDuration => _moveDuration;
    /// <summary> ヒット時に移動を止めるかどうか </summary>
    public bool StopOnHit => _stopOnHit;
    /// <summary> すり抜け攻撃かどうか </summary>
    public bool IsPhantom => _isPhantom;

    /// <summary> ヒットストップの設定 </summary>

    public HitStopData HitStopData => _hitStopData;

    /// <summary> 攻撃アニメーションのステート名 </summary>
    public string AnimationStateName => _animationStateName;
    /// <summary> 攻撃アニメーションの遷移時間（秒）。-1の場合はデフォルト値(0.1f)を使用 </summary>
    public float TransitionDuration => _transitionDuration;
    /// <summary> チャージ攻撃用のアニメーションのステート名（チャージ攻撃でない場合は空文字） </summary>
    public string ChargeAnimationStateName => _chargeAnimationStateName;
    /// <summary> チャージ攻撃のアニメーション遷移時間（秒）。-1の場合はデフォルト値(0.1f)を使用 </summary>
    public float ChargeTransitionDuration => _chargeTransitionDuration;

    /// <summary> 地面ヒットSEを鳴らすかどうか </summary>
    public bool PlayGroundHitSE => _playGroundHitSE;

    /// <summary> 雷の攻撃に追加ダメージを与えるかどうか </summary>
    public bool HasAdditionalLightningDamage => _additionalLightningDamages != null && _additionalLightningDamages.Length > 0;
    /// <summary> 雷の追加ダメージのデータ配列 </summary>
    public AdditionalLightningDamageData[] AdditionalLightningDamages => _additionalLightningDamages;

    /// <summary>
    /// 攻撃バリアントのフィールドをデフォルト値にリセットする
    /// </summary>
    public void SetDefaults()
    {
        _damageMultiplier = 1f;
        _attackRange = 1f;
        _attackRadius = 1f;
        _moveCurve = AnimationCurve.Linear(0, 0, 1, 1);
        _transitionDuration = -1f;
        _chargeTransitionDuration = -1f;
    }

    [Header("攻撃バリアントの基本情報")]
    [SerializeField] private string _attackName; // 攻撃名
    [SerializeField] private ChargeLevel _requiredCharge; // 必要な溜めレベル（None, Level1, Level2）

    [Header("攻撃のダメージ")]
    [SerializeField] private float _damageMultiplier = 1; // 攻撃力倍率（例: 1.5 = 150%のダメージ）

    [Header("攻撃の範囲")]
    [SerializeField] private float _attackRange = 1; // 攻撃の届く距離（例: 1.5 = 1.5m先まで攻撃が届く）
    [SerializeField] private float _attackRadius = 1; // 攻撃の当たり判定の半径（例: 0.5 = 攻撃の中心から0.5m以内がヒット範囲）

    [Header("ノックバック")]
    [SerializeField] private bool _enableKnockback = false; // ノックバックを有効にするかどうか
    [SerializeField] private float _knockbackPower = 5f; // ノックバックの強さ
    [SerializeField] private float _knockbackUpward = 0f; // ノックバックの垂直成分

    [Header("ホーミング")]
    [SerializeField] private bool _enableHoming = false; // ホーミングを有効にするかどうか
    [SerializeField] private float _homingRadius = 5f; // ホーミングの探索半径
    [SerializeField] private float _homingAngle = 45f; // ホーミングの探索角度
    [SerializeField] private float _homingStrength = 10f; // ホーミングの強さ（大きいほどターゲットに向かって急激に曲がる）

    [Header("移動")]
    [SerializeField] private bool _enableMovement = false; // 攻撃中の移動を有効にするかどうか
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.Linear(0, 0, 1, 1); // 移動の速度変化を制御するアニメーションカーブ
    [SerializeField] private float _moveDistance = 0f;
    [SerializeField] private float _moveSpeed = 0f;
    [SerializeField] private float _moveDuration = 0f;
    [Tooltip("ヒット時に移動を止めるかどうか")]
    [SerializeField] private bool _stopOnHit = true;
    [Tooltip("すり抜け攻撃かどうか")]
    [SerializeField] private bool _isPhantom = false; // すり抜け攻撃かどうか 

    [Header("ヒットストップ")]
    [SerializeField] private HitStopData _hitStopData;

    [Header("アニメーション")]
    [SerializeField] private string _animationStateName; // Animatorのステート名
    [SerializeField] private float _transitionDuration = -1f; // 遷移時間（秒）。-1の場合はデフォルト値(0.1f)を使用
    [SerializeField] private string _chargeAnimationStateName; //  チャージ攻撃用のAnimatorのステート名（チャージ攻撃でない場合は空文字）
    [SerializeField] private float _chargeTransitionDuration = -1f; // チャージ攻撃の遷移時間（秒）。-1の場合はデフォルト値(0.1f)を使用

    [Header("サウンド")]
    [SerializeField] private bool _playGroundHitSE = false; // 地面ヒットSEを鳴らすか

    [Header("雷の追加ダメージ")]
    [Tooltip("雷の追加ダメージのデータ")]
    [SerializeField] private AdditionalLightningDamageData[] _additionalLightningDamages;
}
