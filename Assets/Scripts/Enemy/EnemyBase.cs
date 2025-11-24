using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyBase : MonoBehaviour , IPoolable
{
    [Header("Enemyのコンポーネント")]
    [SerializeField] private EnemyMove _move;
    [SerializeField] private Animator _animator;
    [Header("データ")]
    [SerializeField] private CharacterData _data;

    [Header("攻撃設定")]//この辺あとで別のクラス作る
    [SerializeField] private float _attackInterval = 2.0f;
    private float _timeSinceLastAttack = 0.0f;

    private Transform _playerTransform;
    protected Transform PlayerTransform => _playerTransform;
    protected EnemyMove Move => _move;
    // 現在のHP
    private float _currentHp;
    public Action OnRelease { get; set; }

    // 外部に通知するためのイベント
    public event Action OnDeath;

    private bool _isDead = false;

    // プレイヤーなど外部参照の初期化
    public virtual void Init(Transform playerTransform)
    {
        gameObject.SetActive(true);
        _playerTransform = playerTransform;
        _move?.Init(playerTransform,10);

        // HP 初期化など
        _currentHp = (_data != null && _data.MaxHP > 0f) ? _data.MaxHP : 1f;
        _isDead = false;
        _timeSinceLastAttack = _attackInterval;

        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
    }
    private void Update()
    {
        if (_playerTransform == null || _isDead) return;

        // 時間更新
        _timeSinceLastAttack += Time.deltaTime;

        // 近接判定は EnemyMove が保持しているフラグを読む
        bool isNear = (_move != null) ? _move.IsNearPlayerFlag : false;

        if (isNear)
        {
            TryAttack();
        }
        //遠ければ EnemyMove が NavMesh で追跡してくれる前提
    }
    private void TryAttack()
    {
        if (_timeSinceLastAttack < _attackInterval) return;

        // アニメやエフェクトあればここで再生
        PlayAttackAnimation();

        AttackAction();

        _timeSinceLastAttack = 0f;
    }
    private void PlayAttackAnimation()
    {
        // アニメーション再生(後回し)
        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
        }
        // 攻撃SEを鳴らす等(後回し)
    }
    protected virtual void AttackAction()
    {
    }
    //ダメージを受ける時に呼ぶ
    public void TakeDamage(float amount)
    {
        if (_isDead) return;
        if (amount <= 0f) return;

        _currentHp -= amount;
        // TODO: ヒットエフェクト、ノックバック等をここで呼ぶ

        if (_currentHp <= 0f)
        {
            Die();
        }
    }

    // 強制的に即死させたいとき（EnemyInstanceManager.ForceClearAllEnemies などから呼ぶ）
    public void ForceKill()
    {
        Debug.Log($"{nameof(EnemyBase)}: ForceKill called on {gameObject.name}");
        Die();
    }

    // 死亡処理（外部に通知 -> オブジェクト破棄 or 無効化）
    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        // 死亡イベントを投げる（登録されているハンドラが EnemyInstanceManager 側でセットされている前提）
        try
        {
            OnDeath?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"OnDeath handler threw exception: {ex}");
        }

        // TODO:エフェクトやスコア加算、音などの処理


        OnRelease?.Invoke();

    }

    // オブジェクトが破棄されるときの後片付け（念のため）
    private void OnDestroy()
    {
        // OnDeath は発火済みのはずだが、保険として null にする
        OnDeath = null;
    }
}
