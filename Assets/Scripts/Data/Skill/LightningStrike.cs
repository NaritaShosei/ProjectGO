using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LightningStrike", menuName = "GameData/Skill/LightningStrike")]
public class LightningStrike : SkillBase
{
    public int TargetCount => _targetCount;
    public float MinInterval => _minInterval;
    public float MaxInterval => _maxInterval;
    public float DamageMultiplier => _damageMultiplier;
    public float SearchRadius => _searchRadius;
    public float AreaRadius => _areaRadius;
    public float ElectricShockDuration => _electricShockDuration;
    public float GrantEffectProbability => _grantEffectProbability;
    public float UpDamagePercentage => _upDamagePercentage;
    public string[] HitEffectKeys => _hitEffectKeys;

    // Timing == Passive のスキルとして CreateUpdater を実装
    public override ISkillUpdater CreateUpdater() => new LightningStrikeUpdater(this);

    [SerializeField] private int _targetCount = 1;
    [SerializeField] private float _minInterval = 2f;
    [SerializeField] private float _maxInterval = 4f;
    [SerializeField] private float _damageMultiplier = 1.6f;
    [SerializeField] private float _searchRadius = 999f;
    [SerializeField] private string[] _hitEffectKeys;
    [SerializeField] private float _areaRadius;
    [SerializeField] private float _electricShockDuration;
    [SerializeField, Range(0, 1)] private float _grantEffectProbability = 1f;
    [SerializeField, Range(0, 1)] private float _upDamagePercentage = 0f;
}
