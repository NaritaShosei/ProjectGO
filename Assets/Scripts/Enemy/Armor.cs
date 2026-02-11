using System;
using TMPro;
using UnityEngine;

/// <summary>
/// モブとボスのアーマーの抽象クラス
/// IsBrokenのboolを使用するかと思ったが、Enemy.EnemyTypeで十分かもしれない
/// </summary>
public abstract class Armor : MonoBehaviour, IArmor
{
    // 鎧破壊時のイベント発行
    // 例えば鎧破壊時エフェクトとかを想定
    public event Action<IEnemy> OnBroken;
    // public bool IsBroken() => _isBroken;

    public void Init(IEnemy enemy)
    {
        _enemy = enemy;

        _stats = new ArmorStats(_data);
        _stats.OnBroken += Broken;

        // _isBroken = false;
    }

    public float AbsorbDamageAndReturnExcess(float damage)
    {
        // ダメージ量 - 現在のHp
        // 制限:0以上
        float excessDamage = Mathf.Max(0, damage - _stats.CurrentHealth);

        _stats.TakeDamage(damage);

        return excessDamage;
    }

    public void Broken()
    {
        // _isBroken = true;
        _stats.OnBroken -= Broken;
        OnBroken?.Invoke(_enemy);

        // 鎧破壊時に非表示　
        // TODO: 参照など残っていないかチェックする
        gameObject.SetActive(false);
    }

    [SerializeField] protected ArmorData _data;

    protected IEnemy _enemy;
    protected ArmorStats _stats;
    // protected bool _isBroken; 
}
