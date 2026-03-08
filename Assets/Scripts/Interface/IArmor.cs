using UnityEngine;
using System;

/// <summary>
/// エネミーからアーマーへの窓口
/// IArmorHealthを継承することでUI等の購読者はIArmorHealthだけを知ればよい
/// </summary>
public interface IArmor : IArmorHealth
{
    /// <summary>
    /// 初期化
    /// 誰の鎧かを登録する
    /// </summary>
    public void Init(IEnemy enemy);

    /// <summary>
    /// ダメージを引き受け、超過ダメージを返す
    /// </summary>
    public float AbsorbDamageAndReturnExcess(float damage);

    /// <summary>
    /// 壊れる
    /// </summary>
    public void Broken();
}
