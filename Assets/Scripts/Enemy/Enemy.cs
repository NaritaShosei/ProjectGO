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
    // TODO: 鎧が壊れた時のActionを登録

    public virtual void Init(IPlayer player)
    {
        _playerTransform = player.GetTargetCenter();

        // 再利用の可能性があるので、AwakeではなくInitで
        // 何度も呼ぶのは微妙かなと思い、Initで呼ぶ方針にしてみました
        _shockCts = new CancellationTokenSource();
    }

    public void AddKnockBackForce(Vector3 direction)
    {
        // ノックバック
    }

    public Transform GetTargetCenter()
    {
        return _targetCenter;
    }

    public virtual void TakeDamage(DamageContext context)
    {
        if (_isDead) { return; }

        int damage = DamageSystem.Calculate(context, _defenceContext);

        // TODO; もし鎧を保持していれば鎧だけにがダメージ
        // TODO: 超過ダメージを生身に流す

        _stats.TakeDamage(damage);
    }

    public async UniTask ActivateShockDebuff(int durationSeconds = 10)
    {
        _defenceContext.HasShockDebuff = true;

        try
        {
            // 10秒後にHasShockDebuffを切り替える
            await UniTask.Delay(
                delayTimeSpan: TimeSpan.FromSeconds(durationSeconds),
                delayType: DelayType.DeltaTime,
                delayTiming: PlayerLoopTiming.Update, // Enemy自体がUpadateを持っているのでUpdateでいいと判断
                cancellationToken: _shockCts.Token
                );
        }
        catch (OperationCanceledException)
        {
            // 死亡時キャンセル
        }

        _defenceContext.HasShockDebuff = false;
    }

    // TODO: _dataの名前を変えたほうがいい？
    [SerializeField] protected EnemyData _data;
    [SerializeField] protected MobArmorData _armorData;　// 鎧データの登録
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
            // TODO: 鎧の登録があれば鎧、なければ生身
            EnemyType = EnemyType.Flesh,

            HasShockDebuff = false
        };

        _stats = new EnemyStats(_data);

        _stats.OnHealthZero += _stats.Kill;

        _stats.OnDead += OnDeath;

        // TODO: 鎧Statsを作成、データの初期化をする
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
    /// 鎧破壊時の処理
    /// </summary>
    protected virtual void OnArmorBroken()
    {
        // TODO: EnemyTypeの変更
        // TODO: 鎧オブジェクトのDestroy or Disable
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
        _shockCts.Cancel();
        _shockCts.Dispose();
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
