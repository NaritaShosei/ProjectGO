public interface IStatUpgradable
{
    void AddAttackPower(float value);
    void AddCriticalRate(float value);
    void AddDefensePower(float value);
    void AddMaxHealth(float value);

    /// <summary> 雷ゲージ上限を増やす（スキル：上限解放） </summary>
    void AddMaxThunderGauge(float value);

    /// <summary> 消費速度を変更する。負の値で軽減（スキル：消費軽減） </summary>
    void AddThunderDrainPerSecond(float delta);

    /// <summary> 回復速度を変更する。正の値で強化（スキル：回復強化） </summary>
    void AddThunderRecoverPerSecond(float delta);
}
