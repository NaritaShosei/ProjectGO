using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// Enemyの基底クラス
/// </summary>
public abstract class Enemy : MonoBehaviour, IEnemy
{
    public event Action<IEnemy> OnDead;
    public event Action<IEnemy> OnArmorBroken;

    public virtual void Init(IPlayer player)
    {
        _playerTransform = player.GetTargetCenter();
    }

    public void AddKnockBackForce(Vector3 direction)
    {
        transform.position += direction;
    }

    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public virtual void TakeDamage(DamageContext context)
    {
        if (_isDead) { return; }

        int damage = DamageSystem.Calculate(context, _defenceContext);

        _stats.TakeDamage(damage);

        // TODO: ここからどう鎧に流すか・・
        // TODO: ひとまずあきらめてMobEnemyにだけ実装した。
    }

    public async UniTask ActivateShockDebuff(int durationSeconds = 10)
    {
        _shockCts?.Cancel();
        _shockCts?.Dispose();
        _shockCts = new CancellationTokenSource();

        _defenceContext.HasShockDebuff = true;

        try
        {
            // 10秒後にHasShockDebuffを切り替える
            await UniTask.Delay(
                delayTimeSpan: TimeSpan.FromSeconds(durationSeconds),
                delayType: DelayType.DeltaTime,
                delayTiming: PlayerLoopTiming.Update, // Enemy自体がUpdateを持っているのでUpdateでいいと判断
                cancellationToken: _shockCts.Token
                );
        }
        catch (OperationCanceledException)
        {
            // 死亡時 OR 感電キャンセル
        }

        _defenceContext.HasShockDebuff = false;
    }

    public abstract void OnConditionInterrupt();

    [SerializeField] protected EnemyData _data;
    [SerializeField] private Transform _targetCenter;

    protected EnemyDefenseContext _defenceContext;
    protected EnemyStats _stats;
    protected Transform _playerTransform;

    protected bool _isDead; // 軽い実装のため bool のフラグを使用

    protected CancellationTokenSource _shockCts;

    protected virtual void Awake()
    {
        // OnDead時の登録
        OnDead += HandleDead;

        // 雑に生身限定
        _defenceContext = new EnemyDefenseContext()
        {
            EnemyType = EnemyType.Flesh,

            HasShockDebuff = false
        };

        _stats = new EnemyStats(_data);

        _stats.OnHealthZero += _stats.Kill;

        _stats.OnDead += OnDeath;

        // 鎧生成関連はすべてMobEnemyのほうで実装
    }
    protected virtual void Update()

    {
        if (_isDead) { return; }
        UpdateEnemy(Time.deltaTime);
    }

    private void OnDestroy()
    {
        _stats.OnHealthZero -= _stats.Kill;

        _stats.OnDead -= OnDeath;

        OnDead -= HandleDead;
    }

    /// <summary>
    /// 死亡時処理
    /// </summary>
    protected virtual void OnDeath()
    {
        if (_isDead) { return; }

        _isDead = true;
        OnDead?.Invoke(this);
        OnDeathInternal();
    }

    protected void HandleDead(IEnemy _)
    {
        // _shockCtsの解除
        _shockCts?.Cancel();
        _shockCts?.Dispose();
    }

    protected virtual void OnDeathInternal()
    {
        Destroy(gameObject);
    }

    protected abstract void UpdateEnemy(float deltaTime);


}

// TODO: 鎧が壊れたことを検知してEnemyTypeを切り替える
public struct EnemyDefenseContext
{
    public EnemyType EnemyType; // 鎧 / 生身
    public bool HasShockDebuff; // 感電弱体化状態か
}

public enum EnemyType
{
    [InspectorName("生身")]
    Flesh,
    [InspectorName("鎧")]
    Armor,
}
