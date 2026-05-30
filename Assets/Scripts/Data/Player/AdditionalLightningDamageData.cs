using System;
using UnityEngine;

[Serializable]
public class AdditionalLightningDamageData
{
    /// <summary> 雷追加ダメージの倍率 </summary>
    public float LightningDamageMultiplier => _lightningDamageMultiplier;
    /// <summary> 雷ダメージ発生までのディレイ（秒）</summary>
    public float LightningDamageDelay => _lightningDamageDelay;

    [Header("雷追加ダメージの倍率。0なら追加ダメージなし（雷神モード専用）")]
    [Min(0f)]
    [SerializeField] private float _lightningDamageMultiplier = 0f;
    [Header("本体ダメージから雷ダメージ発生までのディレイ（秒）")]
    [Min(0f)]
    [SerializeField] private float _lightningDamageDelay = 0.15f;
}
