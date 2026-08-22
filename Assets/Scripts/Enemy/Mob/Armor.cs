using System;
using UnityEngine;

/// <summary>
/// モブとボスのアーマーの抽象クラス
/// ダメージの肩代わり・超過ダメージの返却・破壊通知を担う
/// </summary>
public abstract class Armor : MonoBehaviour, IArmor
{
    public float CurrentHealth => _stats?.CurrentHealth ?? 0f;
    public float MaxHealth => _stats?.MaxHealth ?? 0f;

    /// <summary>
    /// アーマーが破壊されたときに発火するイベント
    /// MobEnemy・UI等が購読する
    /// </summary>
    public event Action OnBroken;

    /// <summary>
    /// HP変化イベント（IArmorHealth越しにUI等が購読する）
    /// _statsはInit()後に生成されるためイベントの中継もInit()内で設定する
    /// </summary>
    event Action<float, float> IArmorHealth.OnHealthChanged
    {
        add => _onHealthChangedHealth += value;
        remove => _onHealthChangedHealth -= value;
    }

    /// <summary>
    /// アーマーを初期化し、ArmorStatsを生成してイベント中継を設定する
    /// </summary>
    public void Init(IEnemy enemy)
    {
        _stats = new ArmorStats(_data);
        _stats.OnBroken += Broken;

        // ArmorStatsのHP変化をIArmorHealth購読者へ中継する
        _stats.OnHealthChanged += (current, max) => _onHealthChangedHealth?.Invoke(current, max);
    }

    /// <summary>
    /// ダメージをアーマーが引き受け、HPを超えた超過分を返す
    /// </summary>
    public float AbsorbDamageAndReturnExcess(float damage)
    {
        float excessDamage = Mathf.Max(0, damage - _stats.CurrentHealth);
        _stats.TakeDamage(damage);
        return excessDamage;
    }

    /// <summary>
    /// アーマーを破壊し、OnBrokenを発火してGameObjectを非表示にする
    /// OnBroken購読者（MobEnemy.BreakArmor）が解除処理を行うため購読解除は不要
    /// </summary>
    public void Broken()
    {
        _stats.OnBroken -= Broken;
        OnBroken?.Invoke();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// IArmorHealth.GetTargetCenter() の実装
    /// </summary>
    public Transform GetTargetCenter()
    {
        return transform;
    }

    /// <summary>
    /// アーマーを復活させる（Down→復帰用）
    /// </summary>
    public virtual void Restore()
    {
        gameObject.SetActive(true);

        _stats = new ArmorStats(_data);
        _stats.OnBroken += Broken;
        _stats.OnHealthChanged += (current, max) =>
        {
            _onHealthChangedHealth?.Invoke(current, max);
        };
    }

    [SerializeField] protected ArmorData _data;

    protected ArmorStats _stats;

    // IArmorHealth.OnHealthChangedの購読者向けイベントの実体
    private Action<float, float> _onHealthChangedHealth;
}
