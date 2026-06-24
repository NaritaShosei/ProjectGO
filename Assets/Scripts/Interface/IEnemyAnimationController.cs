/// <summary>
/// EnemyのアニメーションイベントをSMBから受け取るインターフェース。
/// Player側のIAnimationControllerに相当するEnemy版。
/// EnemyAnimationEventReceiverが実装し、SMBがanimator.TryGetComponent()で取得する。
/// </summary>
public interface IEnemyAnimationController
{
    /// <summary>攻撃ヒットタイミングの通知</summary>
    public void AnimEvent_AttackHit();

    /// <summary>攻撃アニメーション終了の通知</summary>
    public void AnimEvent_AttackEnd();

    /// <summary>Barkアニメーション終了の通知</summary>
    public void AnimEvent_BarkEnd();

    /// <summary>GetUpアニメーション終了の通知</summary>
    public void AnimEvent_GetUpEnd();

    /// <summary>ノックバック（Hit/Small）アニメーション終了の通知</summary>
    public void AnimEvent_KnockbackEnd();

    /// <summary>死亡アニメーション終了の通知</summary>
    public void AnimEvent_DeadEnd();

    /// <summary>ダウンアニメーション終了の通知 </summary>
    public void AnimEvent_DownEnd();

    /// <summary>アタックエフェクトの再生タイミング通知</summary>
    public void AnimEvent_AttackEffect();
}
