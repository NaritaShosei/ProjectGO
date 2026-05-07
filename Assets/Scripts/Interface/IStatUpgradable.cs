public interface IStatUpgradable
{
    void AddMaxHealth(float value);

    /// <summary> 雷ゲージ上限を増やす（スキル：上限解放） </summary>
    void AddMaxThunderGauge(float value);
}
