using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private const float INPUT_THRESHOLD = 0.001f;

    private PlayerManager _manager;
    private InputHandler _input;
    private Animator _animator;
    private CharacterData _data;

    [Header("Rigidbody")]
    [SerializeField] private Rigidbody _rb;

    [Header("回転方向への補間率 (0〜1)")]
    [SerializeField, Range(0, 1)] private float _rotateSmooth = 0.5f;

    [Header("回避攻撃に派生可能な時間")]
    [SerializeField] private float _canDodgeAttackLimit = 0.5f;

    /// <summary>
    /// 移動可能な時は入力を反映
    /// </summary>
    private Vector2 _moveInput => _manager.CanMove ? _input.MoveInput : Vector2.zero;

    private CancellationTokenSource _cts;

    public void Init(PlayerManager manager, InputHandler input, Animator animator, CharacterData data)
    {
        _manager = manager;
        _input = input;
        _animator = animator;
        _data = data;

        _input.OnDodge += Dodge;
    }

    private void Update()
    {
        Move();
        RotateToCameraForward();
        MoveAnimation();
    }

    private void Move()
    {
        // 回避中のみvelocityの上書きを避けたいため早期リターン
        if (_manager.HasFlag(PlayerStateFlags.Dodging)) { return; }

        Vector2 vel = _moveInput;
        Camera camera = _manager.MainCamera;

        // 入力がゼロに近いなら移動なし
        float inputMag = vel.magnitude;
        if (inputMag < INPUT_THRESHOLD)
        {
            _rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 right = camera.transform.right * vel.x;
        Vector3 forward = camera.transform.forward * vel.y;

        // 方向だけ正規化
        Vector3 moveDir = (right + forward).normalized;
        moveDir.y = 0f;

        // 入力の大きさを速度に反映
        float speed = _data.MoveSpeed * inputMag;

        _rb.linearVelocity = moveDir * speed;
    }


    private void RotateToCameraForward()
    {
        Vector2 vel = _moveInput;

        // 入力がゼロに近いときは回転しない
        if (vel.magnitude < INPUT_THRESHOLD) { return; }

        Camera camera = _manager.MainCamera;

        // 前後左右の移動すべて正面を向くようにするためカメラの正面のみ考慮
        Vector3 camForward = camera.transform.forward;

        camForward.y = 0;

        // 線形補間で滑らかに回転
        var dir = Vector3.Lerp(transform.forward, camForward.normalized, _rotateSmooth);

        transform.rotation = Quaternion.LookRotation(dir);
    }

    private void MoveAnimation()
    {
        Vector2 vel = _moveInput;

        // BlendTreeに関する値
        _animator.SetFloat("MoveRight", vel.x);
        _animator.SetFloat("MoveForward", vel.y);

        // 遷移条件
        float speed = vel.magnitude;
        _animator.SetFloat("Speed", speed);
    }

    private async void Dodge()
    {
        if (!_manager.TryDodge(_data.DodgeStamina)) return;

        Vector2 input = _input.MoveInput;
        Vector3 dodgeDir;

        if (input.magnitude > INPUT_THRESHOLD)
        {
            Camera camera = _manager.MainCamera;
            Vector3 right = camera.transform.right * input.x;
            Vector3 forward = camera.transform.forward * input.y;
            dodgeDir = (right + forward).normalized;
        }
        else
        {
            dodgeDir = transform.forward;
        }

        dodgeDir.y = 0f;

        if (_cts is not null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        _cts = new CancellationTokenSource();

        // 回避アニメーションはここで実行

        try
        {
            await DodgeAsync(dodgeDir, _cts.Token);
        }

        catch (OperationCanceledException)
        {
            // 正常キャンセル
        }

        catch (Exception ex)
        {
            Debug.LogError($"回避キャンセル中にエラー：{ex}");
        }
    }

    private async UniTask DodgeAsync(Vector3 direction, CancellationToken token)
    {
        _manager.AddFlags(PlayerStateFlags.Dodging | PlayerStateFlags.Invincible);

        Debug.Log("回避中");

        float dodgeSpeed = _data.DodgeSpeed;
        float duration = _data.DodgeDuration;

        if (_data.DodgeClip)
        {
            duration += _data.DodgeClip.length;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            _rb.linearVelocity = direction * dodgeSpeed;
            elapsed += Time.deltaTime;
            await UniTask.Yield(token);
        }

        _rb.linearVelocity = Vector3.zero;

        _manager.RemoveFlags(PlayerStateFlags.Dodging);
        _manager.AddFlags(PlayerStateFlags.CanDodgeAttack);

        Debug.Log("回避動作終了、無敵時間開始、回避攻撃派生可能");

        UniTask[] tasks = { ResetCanDodgeAttackAsync(), ResetInvincibleAsync() };

        await UniTask.WhenAll(tasks);

        Debug.Log("回避完全終了");
    }

    private async UniTask ResetInvincibleAsync()
    {
        // 無敵時間
        if (_data.InvincibleDuration > 0)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(_data.InvincibleDuration),
                cancellationToken: destroyCancellationToken
            );
        }

        // 無敵終了
        _manager.RemoveFlags(PlayerStateFlags.Invincible);
    }

    private async UniTask ResetCanDodgeAttackAsync()
    {
        // 回避攻撃可能時間時間
        if (_data.CanDodgeAttackDuration > 0)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(_data.CanDodgeAttackDuration),
                cancellationToken: destroyCancellationToken
            );
        }

        // 回避攻撃可能終了
        _manager.RemoveFlags(PlayerStateFlags.CanDodgeAttack);
    }

    private void OnDestroy()
    {
        if (_cts is null) { return; }
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }
}
