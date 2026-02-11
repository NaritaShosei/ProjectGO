using UnityEngine;
using System;

/// <summary>
/// エネミーからアーマーへの窓口
/// </summary>
public interface IArmor
{
    /// <summary>
    /// 鎧破壊時に発火するイベント保持用
    /// </summary>
    public event Action<IEnemy> OnBroken;

    /// <summary>
    /// 鎧が壊れているか
    /// </summary>
    // public bool IsBroken();

    /// <summary>
    /// 初期化できる
    /// ・誰の鎧かを登録
    /// </summary>
    /// <param name="enemy"></param>
    public void Init(IEnemy enemy);

    /// <summary>
    /// ダメージを引き受け、超過ダメージを通知する
    /// </summary>
    /// <param name="damage"></param>
    /// <returns>残りダメージ量</returns>
    public int AbsorbDamageAndReturnExcess(int damage);

    /// <summary>
    /// 壊れる
    /// </summary>
    public void Broken();
}
