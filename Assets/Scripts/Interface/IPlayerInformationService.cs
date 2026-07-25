using UnityEngine;

public interface IPlayerInformationService
{
    /// <summary> 情報源となるPlayer </summary>
    public IPlayer Player { get; }

    /// <summary> Playerが生きているかの判定 </summary>
    public bool IsPlayerAlive();

    /// <summary> Playerが接敵中か(正面に敵がいるか)の判定 </summary>
    /// <returns> 接敵中であればtrue、そうでなければfalse </returns>
    public bool IsPlayerEncounteringEnemy(float playerViewRange = 20f, float playerViewAngle = 120f);

    /// <summary> 対象がPlayerの後ろにいるのか前にいるのかの情報取得メソッド </summary>
    /// <param name="targetTransform"> 後ろにいるか測定する対象の位置 </param>
    /// <returns> targetTransformがPlayerから見て背後にいればtrue、前ならfalse </returns>
    public bool IsBehaindPlayer(Transform targetTransform, float playerViewAngle = 120f);

    /// <summary> Playerとの距離を取得する </summary>
    /// <param name="targetPosition"> 距離を測定する対象の位置 </param>
    /// <returns> Playerとの距離 </returns>
    public float ToPlayerDistance(Vector3 targetPosition);

    /// <summary> Playerに対してDamageを与える処理 </summary>
    /// <param name="damage"> Damage量 </param>
    public void TakeDamage(float damage);
    public void TakeDamage(float damage, DamageReactionType reactionType)
    {
        Player.TakeDamage(damage, reactionType);
    }
}
