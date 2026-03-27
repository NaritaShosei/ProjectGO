using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

/// <summary>
/// 死亡時のアニメーション遷移と物理ノックバックをカプセル化するCondition。
/// StartDeathKnockbackAsync() の役割を Condition として切り出したもの。
/// 設計上の注意:
/// ・_isDead = true 後は ConditionController.Tick() が呼ばれないため Tick() は no-op
/// ・物理ループは OnEnter() 内で UniTask として起動し自律動作する
/// ・OnExit() で物理ループをキャンセルする（ObjectPool返却時の再利用に対応）
/// ・KnockbackCondition と同じく AddKnockbackForce / SetPosition で位置を操作する
/// </summary>
public sealed class DeadCondition : IEnemyCondition
{
    public ConditionType Type => ConditionType.Dead;
    public bool BlocksAction => true;
    public bool IsFinished => false;

    /// <param name="lastHitDirection">最後に受けたダメージの方向ベクトル</param>
    /// <param name="data">EnemyData（ノックバック強度・重力等のパラメータ取得用）</param>
    /// <param name="ct">MonoBehaviour の destroyCancellationToken を渡す</param>
    public DeadCondition(Vector3 lastHitDirection, EnemyData data, CancellationToken ct)
    {
        var dirXZ = new Vector3(lastHitDirection.x, 0f, lastHitDirection.z);
        _velocityH = dirXZ.sqrMagnitude > 0f
            ? dirXZ.normalized * data.DeathKnockbackPower
            : Vector3.zero;
        _velocityV = data.DeathKnockbackUpward;
        _deceleration = data.KnockbackDeceleration;
        // 水平のみ・上方向のみどちらの設定でも物理ループを起動する
        _hasPhysics = data.DeathKnockbackPower > 0f || data.DeathKnockbackUpward > 0f;
        // destroyCancellationToken とリンクし、OnExit() または Destroy() どちらでもキャンセルできるようにする
        _physicsCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    }

    /// <summary>
    /// SetDead() を即座に発火し、物理ノックバックが必要な場合は UniTask を起動する。
    /// </summary>
    public void OnEnter(IEnemy enemy)
    {
        enemy.EnemyAnimator?.SetDead();

        if (_hasPhysics)
            RunPhysicsAsync(enemy).Forget();
    }

    /// <summary>
    /// _isDead = true 後は呼ばれないため no-op。物理は UniTask 内で完結する。
    /// </summary>
    public void Tick(IEnemy enemy, float dt) { }

    /// <summary>
    /// 物理ループをキャンセルする。ObjectPool返却時（ReInitialize経由）に呼ばれる。
    /// </summary>
    public void OnExit(IEnemy enemy)
    {
        _physicsCts?.Cancel();
        _physicsCts?.Dispose();
        _physicsCts = null;
    }

    private readonly Vector3 _velocityH;
    private readonly float _velocityV;
    private readonly float _deceleration;
    private readonly bool _hasPhysics;
    private CancellationTokenSource _physicsCts;

    private const float _gravity = -30f;

    /// <summary>
    /// KnockbackCondition と同構造の物理ループ。
    /// Time.deltaTime * enemy.TimeScale を使用するため HitStop に対応している。
    /// </summary>
    private async UniTaskVoid RunPhysicsAsync(IEnemy enemy)
    {
        var velocityH = _velocityH;
        float velocityV = _velocityV;
        float groundY = enemy.Position.y;

        while (true)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, _physicsCts.Token);

            float dt = Time.deltaTime * enemy.TimeScale;

            // 水平減速
            float hSpeed = velocityH.magnitude;
            if (hSpeed > 0f)
            {
                float newSpeed = Mathf.Max(0f, hSpeed - _deceleration * dt);
                velocityH = newSpeed > 0f ? velocityH.normalized * newSpeed : Vector3.zero;
            }

            // 垂直（重力）
            velocityV += _gravity * dt;

            // 移動適用
            enemy.AddKnockbackForce(velocityH * dt + Vector3.up * (velocityV * dt));

            // 地面クランプ（めり込み防止）
            if (enemy.Position.y < groundY)
            {
                var pos = enemy.Position;
                pos.y = groundY;
                enemy.SetPosition(pos);
                velocityV = 0f;
            }

            // 停止判定（地面に接触 かつ 水平速度がゼロ）
            if (enemy.Position.y <= groundY && velocityH.sqrMagnitude < 0.0001f)
                break;
        }
    }
}
