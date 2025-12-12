using DG.Tweening;
using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = System.Action;

[RequireComponent(typeof(Collider), (typeof(Rigidbody)))]
public class EnemyBase : MonoBehaviour, IPoolable, IEnemy, ISpeedChange
{
    [Header("Enemyのコンポーネント")]
    [SerializeField] private EnemyMove _move;
    [SerializeField] private Animator _animator;
    [Header("データ")]
    [SerializeField] protected CharacterData CharacterData;
    [SerializeField] protected AttackData AttackData;

    [Header("攻撃設定")]//この辺あとで別のクラス作る
    [SerializeField] private float _stunInterval = 2.0f;
    [SerializeField] private float _attackInterval = 2.0f;

    [Header("マテリアル")]
    [SerializeField] protected Material _defaultMaterial;
    [SerializeField] protected Material _damagedMaterial;

    private float _timeSinceLastAttack = 0.0f;
    private float _stunTimer = 0.0f;
    private float _beforePosY = 0.0f;

    private Rigidbody _rb;
    private Transform _playerTransform;
    protected Transform PlayerTransform => _playerTransform;
    protected EnemyMove Move => _move;
    protected EnemyInstanceManager InstanceManager { get; private set; }
    // 現在のHP
    private float _currentHp;
    public Action OnRelease { get; set; }
    public float TimeScale { get; set; } = 1.0f;

    // 外部に通知するためのイベント
    public event Action OnDeath;

    private bool _isStunned = false;
    private bool _isDead = false;

    private NavMeshAgent _agent;
    private Renderer[] _renderers;
    private void OnDisable()
    {
        if (InstanceManager != null)
        {
            InstanceManager.UnRegisterSpeed(this);
        }
    }
    // プレイヤーなど外部参照の初期化
    public virtual void Init(Transform playerTransform, EnemyInstanceManager manager)
    {
        if (AttackData == null)
        {
            Debug.LogError($"{nameof(EnemyBase)}: AttackData is not assigned on {gameObject.name} Prefab");
        }
        if (CharacterData == null)
        {
            Debug.LogError($"{nameof(EnemyBase)}: CharacterData is not assigned on {gameObject.name} Prefab");
        }
        if(_renderers == null)
        {
            _renderers = gameObject.GetComponentsInChildren<Renderer>();
            if(_defaultMaterial == null || _damagedMaterial == null)
            {
                Debug.LogError("マテリアルをセットしてください。");
                UnityEditor.EditorApplication.isPlaying = false;
            }
        }
        gameObject.SetActive(true);
        _playerTransform = playerTransform;
        _move?.Init(playerTransform, CharacterData.MoveSpeed);
        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody>();
        }
        // HP 初期化など
        _currentHp = (CharacterData != null && CharacterData.MaxHP > 0f) ? CharacterData.MaxHP : 1f;
        _isDead = false;
        _timeSinceLastAttack = _attackInterval;
        _stunTimer = _stunInterval;
        _agent = GetComponent<NavMeshAgent>();
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
        if (TryGetComponent<BehaviorGraphAgent>(out var behaviorGraphAgent))
        {
            behaviorGraphAgent.SetVariableValue("PlayerTransform", _playerTransform);
            behaviorGraphAgent.SetVariableValue("Enemy", this);
        }
    }
    private void FixedUpdate()
    {
        if(_isStunned)
        {
            Debug.Log($"{gameObject.name}がスタンから回復するまであと{_stunTimer} 秒");
            _stunTimer -= Time.fixedDeltaTime * TimeScale;
            Debug.Log(Mathf.Abs(transform.position.y - _beforePosY));
            if (_stunTimer < 0 && Mathf.Abs(transform.position.y - _beforePosY) < 0.0001f)
            {
                _isStunned = false;
                if (_agent != null)
                {
                    _agent.enabled = true;
                }
                Debug.Log("スタン回復");
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _renderers[i].material = _defaultMaterial;
                }
            }
            _beforePosY = transform.position.y;
        }
    }
    private void Update()
    {
        //スタン中の処理
        if (_isStunned)
        {
            return;
        }
        if (_playerTransform == null || _isDead) return;

        // 時間更新
        _timeSinceLastAttack += Time.deltaTime * TimeScale;

        // 近接判定は EnemyMove が保持しているフラグを読む
        bool isNear = (_move != null) ? _move.IsNearPlayerFlag : false;

        if (isNear)
        {
            TryAttack();
        }
        //遠ければ EnemyMove が NavMesh で追跡してくれる前提
    }
    public void TryAttack()
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

    public void AddKnockBackForce(Vector3 direction)
    {
        Debug.Log($"敵がノックバック{direction}");
        _rb.linearVelocity = direction;
        if (_agent != null)
        {
            _agent.enabled = false;
        }
        _isStunned = true;
        _stunTimer = _stunInterval;
        _beforePosY = transform.position.y;
    }
    // ダメージを受ける時に呼ぶ
    public void AddDamage(float amount)
    {
        if (_isDead) return;
        if (amount <= 0f) return;

        _currentHp -= amount;
        Debug.Log($"{nameof(EnemyBase)}: {gameObject.name} took {amount} damage, current HP: {_currentHp}");
        // TODO: ヒットエフェクト、ノックバック等をここで呼ぶ

        if (_currentHp <= 0f)
        {
            Die();
        }
        for(int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].material = _damagedMaterial;
        }
        
    }

    public Transform GetTargetCenter()
    {
        return transform;
    }

    public void OnSpeedChange(float scale)
    {
        Debug.Log($"{gameObject.name}のスピード倍率:{scale}");
        if (_move != null)
        {
            _move.SetNavMeshData(CharacterData.MoveSpeed * scale);
        }
        TimeScale = scale;
    }
}
