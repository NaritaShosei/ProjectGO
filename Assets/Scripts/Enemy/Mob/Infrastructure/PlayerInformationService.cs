using System;
using UnityEngine;

/// <summary> Playerに関する情報をEnemy向けに提供するサービス </summary>
public class PlayerInformationService : IPlayerInformationService
{
    public PlayerInformationService(IPlayer player, EnemyManager enemyManager)
    {
        _player = player;
        _enemyManager = enemyManager;
    }

    /// <summary> 情報源となるPlayer </summary>
    public IPlayer Player => _player;

    /// <summary>
    /// Playerに攻撃可能かの判定
    /// </summary>
    /// <returns></returns>
    public bool CanAttackPlayer()
    {
        return _player.CurrentHealth > 0f
            && !_player.IsDown;
    }

    /// <summary> Playerが生きているかの判定 </summary>
    /// <returns> 生きていればtrue、そうでなければfalse </returns>
    public bool IsPlayerAlive()
    {
        return _player.CurrentHealth > 0f;
    }

    /// <summary> Playerが接敵中か(正面に敵がいるか)の判定 </summary>
    /// <returns> 接敵中であればtrue、そうでなければfalse </returns>
    public bool IsPlayerEncounteringEnemy(float playerViewRange = 20f, float playerViewAngle = 120f)
    {
        foreach (var enemy in _enemyManager.EnemiesTransformList)
        {
            if (!IsBehaindPlayer(enemy, playerViewAngle) && ToPlayerDistance(enemy.position) <= playerViewRange) return true;
        }

        return false;
    }

    /// <summary> 対象がPlayerの背後かの判定メソッド </summary>
    /// <param name="targetTransform"> 後ろにいるか測定する対象の位置 </param>
    /// <param name="playerViewAngle"> Playerの判定用視野角 </param>
    /// <returns> targetTransformがPlayerから見て背後にいればtrue、前ならfalse </returns>
    public bool IsBehaindPlayer(Transform targetTransform, float playerViewAngle = 120f)
    {
        if (targetTransform == null) return false;

        // PlayerからtargetTransformへの方向ベクトルを計算
        Vector3 directionToTarget = (targetTransform.transform.position - _player.GetTargetCenter().position).normalized;

        // Playerの前方向とターゲットへの方向の内積を計算
        float dot = Vector3.Dot(_player.GetTargetCenter().forward, directionToTarget);

        // ViewAngleの半分の角度のcos値を閾値とする。
        float threshold = Mathf.Cos((playerViewAngle * 0.5f) * Mathf.Deg2Rad);

        // dotがthresholdより小さい場合、targetTransformはPlayerの背後にいると判定する
        if (dot < threshold) return true;
        return false;
    }

    /// <summary> Playerとの距離を取得する </summary>
    /// <param name="targetPosition"> 距離を測定する対象の位置 </param>
    /// <returns> Playerとの距離 </returns>
    public float ToPlayerDistance(Vector3 targetPosition)
    {
        float distance = 0;

        // Playerの中心位置を取得して距離を計算する
        Vector3 playerPos = _player.GetTargetCenter().position;
        distance = Vector3.Distance(playerPos, targetPosition);

        return distance;
    }

    /// <summary> Playerに対してDamageを与える処理 </summary>
    /// <param name="damage"> Damage量 </param>
    public void TakeDamage(float damage)
    {
        _player.TakeDamage(damage);
    }

    private IPlayer _player = null;
    private EnemyManager _enemyManager = null;

    private bool _isPlayerAlive = true;
}
