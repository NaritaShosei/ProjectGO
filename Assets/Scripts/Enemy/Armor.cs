using System;
using UnityEngine;

/// <summary>
/// モブとボスのアーマーの抽象クラス
/// </summary>
public abstract class Armor : MonoBehaviour, IArmor
{
    // IArmorHealth: 破壊イベント（UI等・MobEnemy共通）
    public event Action OnBroken;

    // IArmorHealth: HP変化イベント
    // _statsはInit()後に生成されるためイベントの中継もInit()内で設定する
    event Action<float, float> IArmorHealth.OnHealthChanged
    {
        add => _onHealthChangedHealth += value;
        remove => _onHealthChangedHealth -= value;
    }

    // IArmorHealth.OnHealthChangedの購読者向けイベントの実体
    private Action<float, float> _onHealthChangedHealth;

    public void Init(IEnemy enemy)
    {
        _enemy = enemy;
        _stats = new ArmorStats(_data);
        _stats.OnBroken += Broken;

        // ArmorStatsのHP変化をIArmorHealth購読者へ中継する
        _stats.OnHealthChanged += (current, max) => _onHealthChangedHealth?.Invoke(current, max);
    }

    public float AbsorbDamageAndReturnExcess(float damage)
    {
        float excessDamage = Mathf.Max(0, damage - _stats.CurrentHealth);
        _stats.TakeDamage(damage);
        return excessDamage;
    }

    public void Broken()
    {
        _stats.OnBroken -= Broken;

        // 購読者（UI等・MobEnemy）へ通知する
        OnBroken?.Invoke();

        // 鎧破壊時に非表示
        // TODO: 参照など残っていないかチェックする
        gameObject.SetActive(false);
    }

    // IArmorHealth.GetTargetCenter() の実装
    public Transform GetTargetCenter()
    {
        return transform;
    }

    [SerializeField] protected ArmorData _data;

    protected IEnemy _enemy;
    protected ArmorStats _stats;
}
