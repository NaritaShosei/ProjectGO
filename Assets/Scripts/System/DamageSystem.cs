using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// Boss関連
using BossEnemy.Enum;
using BossEnemy.Character;

public class DamageSystem
{
    private const float DAMAGE_REDUCTION_RATE_BASE = 0.01f;
    private const int DEFENSE_CONSTANT = 100;
    private const int MIN_DAMAGE = 1;
    private const float DEFAULT_ARMOR_DAMAGE_MULTIPLIER = 1.5f;
    private const float DEFAULT_FLESH_DAMAGE_MULTIPLIER = 0.8f;
    private const string SETTINGS_ADDRESS = "DamageSystemSettings";

    public static int CalculateDamage(
        DamageContext attack,
        EnemyDefenseContext defense)
    {
        // クリティカルならその分の攻撃力をAttackPowerに上乗せする
        if (attack.IsCritical) attack.AttackPower = GetCriticalAttackPower(attack);

        // 感電デバフ
        if (defense.HasShockDebuff)
        {
            float upDamage = attack.AttackPower * attack.ElectricShock.UpDamagePercentage;
            attack.AttackPower += upDamage;
        }

        // 合計ダメージを割り出す
        float damage = attack.AttackPower * GetEnemyDefenseTypeMultiplier(attack.PlayerMode, defense.EnemyType);

        // 返り値
        return Mathf.RoundToInt(damage);
    }


    /// <summary> 攻撃のダメージ計算処理 </summary>
    /// <param name="bodyDefense"> Bossの各所の肉質 </param>
    /// <param name="damageContext"> Bossに対する攻撃情報 </param>
    /// <param name="isPlayerModeAddDamage"> Trueの際にPlayerModeによってダメージの変動を行う </param>
    /// <param name="damageHitPlaceType"> ダメージが当たった場所のEnemyDefenseType </param>
    /// <returns> 合計ダメージ </returns>
    public static int CalculateDamage(int bodyDefense, DamageContext damageContext,
        bool isPlayerModeAddDamage = false, EnemyDefenceType damageHitPlaceType = EnemyDefenceType.Flesh)
    {
        // 合計ダメージの変数
        float totalDamage;

        // PlayerのModeによって発生する追加ダメージ
        float playerModeAddDamage = 1;

        // ダメージの軽減率を割り出す
        float damageReductionRate = DAMAGE_REDUCTION_RATE_BASE * bodyDefense;

        // クリティカルならその分の攻撃力をAttackPowerに上乗せする
        if (damageContext.IsCritical) damageContext.AttackPower = GetCriticalAttackPower(damageContext);

        // isPlayerModeAddDamageがTrueならPlayerのModeによってダメージを割合を上下させる
        if (isPlayerModeAddDamage) playerModeAddDamage = GetEnemyDefenseTypeMultiplier(damageContext.PlayerMode, damageHitPlaceType);

        // 合計ダメージを割り出す
        totalDamage = damageContext.AttackPower * damageReductionRate * playerModeAddDamage;

        // 返り値
        return (int)totalDamage;
    }

    public static int ApplyDamageReduction(
     float damage,
     float defensePower)
    {
        float reductionRate =
            defensePower / (defensePower + DEFENSE_CONSTANT);

        return Mathf.RoundToInt(Mathf.Max(
            MIN_DAMAGE, damage * (1f - reductionRate)));
    }

    /// <summary> BossEnemyの被弾場所の硬度(肉質)を割り出す </summary>
    /// <param name="partsType"> 被弾場所 </param>
    /// <param name="bossEnemyData"> 被弾したBossEnemyのData </param>
    /// <returns> 被弾場所の硬度(肉質) </returns>
    public static int GetHitPartsDefense(BodysDefensesType partsType, Status bossEnemyData)
    {
        switch (partsType)
        {
            case BodysDefensesType.None:
                Debug.LogError("PartsNone");
                break;
            case BodysDefensesType.Hard:
                return bossEnemyData.HardSpotsDefense;
            case BodysDefensesType.Normal:
                return bossEnemyData.NormalSpotsDefense;
            case BodysDefensesType.WeekPoint:
                return bossEnemyData.WeekPointDefense;
            case BodysDefensesType.VitalPoint:
                return bossEnemyData.VitalPointDefense;
        }

        return 0;
    }

    /// <summary> BossEnemyの被弾場所の鎧の硬度(肉質)を割り出す </summary>
    public static int GetHitPartsArmorDefense(ArmorAttachmentType attachmentPointsType, Status bossEnemyData)
    {
        switch (attachmentPointsType)
        {
            case ArmorAttachmentType.None:
                Debug.LogError("PartsNone");
                break;
            case ArmorAttachmentType.LeftArm:
                return bossEnemyData.LeftArmArmer.Defense;
            case ArmorAttachmentType.RightArm:
                return bossEnemyData.RightArmArmer.Defense;
            case ArmorAttachmentType.LeftLeg:
                return bossEnemyData.LeftLegArmer.Defense;
            case ArmorAttachmentType.RightLeg:
                return bossEnemyData.RightLegArmer.Defense;
        }

        return 0;
    }

    private static DamageSystemSettings _settings;
    private static bool _loadFailed;

    /// <summary>
    /// シーン読み込み前にAddressablesからダメージ設定を読み込む。
    /// Domain Reloadを無効にしたEditor再生でも前回の状態を引き継がないよう、
    /// キャッシュと失敗状態を初期化してからロードする。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        _settings = null;
        _loadFailed = false;
        LoadSettings();
    }

    /// <summary>
    /// Addressable設定から、プレイヤーモードと防御種別に対応するダメージ倍率を取得する。
    /// 設定を読み込めない環境では変更前の固定値を返す。
    /// </summary>
    private static float GetEnemyDefenseTypeMultiplier(PlayerMode mode, EnemyDefenceType type)
    {
        DamageSystemSettings settings = LoadSettings();
        if (settings != null)
        {
            return settings.GetMultiplier(mode, type);
        }

        // Addressableを利用できない単体テストなどでは、変更前と同じ固定値で計算を継続する。
        switch (mode)
        {
            case PlayerMode.Warrior:
            case PlayerMode.Thunder:
                switch (type)
                {
                    case EnemyDefenceType.Armor: return DEFAULT_ARMOR_DAMAGE_MULTIPLIER;
                    case EnemyDefenceType.Flesh: return DEFAULT_FLESH_DAMAGE_MULTIPLIER;
                }
                break;
        }

        return 1.0f; // 保険
    }

    /// <summary>
    /// ダメージ設定をAddressablesから同期的に読み込み、以降の計算で再利用する。
    /// 既存のダメージ計算APIを同期処理のまま維持するため、初回のみ完了を待機する。
    /// </summary>
    private static DamageSystemSettings LoadSettings()
    {
        if (_settings != null || _loadFailed) return _settings;

        AsyncOperationHandle<DamageSystemSettings> handle =
            Addressables.LoadAssetAsync<DamageSystemSettings>(SETTINGS_ADDRESS);
        _settings = handle.WaitForCompletion();

        if (_settings == null)
        {
            _loadFailed = true;
            Debug.LogError($"Addressable '{SETTINGS_ADDRESS}' のロードに失敗しました。従来値で計算します。");
        }

        return _settings;
    }

    /// <summary> 攻撃がCriticalの際の攻撃力を渡すメソッド </summary>
    private static float GetCriticalAttackPower(DamageContext attack)
    {
        if (!attack.IsCritical) return attack.AttackPower;
        return attack.AttackPower * attack.CriticalMultiplier;
    }
}
