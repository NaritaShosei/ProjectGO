using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Infrastructure;

// Boss関連
using BossEnemy.Enum;
using BossEnemy.Data;

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
    public static int GetHitPartsDefense(BodysDefensesType partsType, BossEnemyData bossEnemyData)
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
    public static int GetHitPartsArmorDefense(ArmorAttachmentPointType attachmentPointsType, BossEnemyData bossEnemyData)
    {
        switch (attachmentPointsType)
        {
            case ArmorAttachmentPointType.None:
                Debug.LogError("PartsNone");
                break;
            case ArmorAttachmentPointType.LeftArm:
                return bossEnemyData.LeftArmArmer.Defense;
            case ArmorAttachmentPointType.RightArm:
                return bossEnemyData.RightArmArmer.Defense;
            case ArmorAttachmentPointType.LeftLeg:
                return bossEnemyData.LeftLegArmer.Defense;
            case ArmorAttachmentPointType.RightLeg:
                return bossEnemyData.RightLegArmer.Defense;
        }

        return 0;
    }

    private static DamageSystemSettings _settings;

    /// <summary>
    /// シーン読み込み前に共通のAssetsLoaderを使用してダメージ設定の読み込みを開始する。
    /// Domain Reloadを無効にしたEditor再生でも前回の状態を引き継がないよう、
    /// キャッシュを初期化してからロードする。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        _settings = null;
        LoadSettingsAsync().Forget();
    }

    /// <summary>
    /// Addressable設定から、プレイヤーモードと防御種別に対応するダメージ倍率を取得する。
    /// 設定を読み込めない環境では変更前の固定値を返す。
    /// </summary>
    private static float GetEnemyDefenseTypeMultiplier(PlayerMode mode, EnemyDefenceType type)
    {
        if (_settings != null)
        {
            return _settings.GetMultiplier(mode, type);
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
    /// ダメージ設定を共通のAssetsLoader経由で非同期に読み込み、以降の計算で再利用する。
    /// 読み込みに失敗した場合は例外を記録し、ダメージ計算では変更前の固定値を使用する。
    /// </summary>
    private static async UniTask LoadSettingsAsync()
    {
        try
        {
            _settings = await AssetsLoader.LoadAssetAsync<DamageSystemSettings>(SETTINGS_ADDRESS);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Addressable '{SETTINGS_ADDRESS}' のロードに失敗しました。従来値で計算します。\n{exception}");
        }
    }

    /// <summary> 攻撃がCriticalの際の攻撃力を渡すメソッド </summary>
    private static float GetCriticalAttackPower(DamageContext attack)
    {
        if (!attack.IsCritical) return attack.AttackPower;
        return attack.AttackPower * attack.CriticalMultiplier;
    }
}
