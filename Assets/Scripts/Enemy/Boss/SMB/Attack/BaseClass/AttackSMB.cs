using BossEnemy.Animation;
using BossEnemy.Attack;
using BossEnemy.Character;
using BossEnemy.Effect;
using BossEnemy.Enum;
using BossEnemy.Interface;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace BossEnemy.SMB
{
    public abstract class AttackSMB : BossCharacterSMB
    {
        public int AttackID => _attackID;

        public void Init(
            IBossCharacterAnimationEventReceiver bossEnemyAnimationEventReceiver,
            IBossEnemyCharacterView bossEnemyCharacterView,
            IAttackHitAreaSpawner attackHitAreaSpawner,
            EffectManager effectManager,
            CameraManager cameraManager,
            Transform bossEnemyTransform,
            IPlayer attackTarget)
        {
            base.Init(bossEnemyAnimationEventReceiver, bossEnemyCharacterView, bossEnemyTransform);
            _effectManager = effectManager;
            _cameraManager = cameraManager;
            _attackHitAreaSpawner = attackHitAreaSpawner;
            _attackTarget = attackTarget;
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if( _attackStartTime <= stateInfo.length)
            {
                NotifyAttackEnd();
                return;
            }

            base.OnStateEnter(animator, stateInfo, layerIndex);

            // 攻撃命中済みフラグをfalseに
            _wasHitAttack = false;

            // 攻撃開始済みフラグ
            _isAttackPlayed = false;

            // CancellationTokenSourceの初期化
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new();

            // 攻撃ヒットイベントの発火を検知できるようにする
            _animationEventReceiver.OnHitAttack += HandleAttackHit;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _elapsedTime += Time.deltaTime * _timeScale;

            if(_elapsedTime >= _attackStartTime && !_isAttackPlayed)
            {
                // 攻撃を開始する
                PlayAttackAsync(_cts.Token).Forget();

                // 攻撃開始済みフラグをTrueにする
                _isAttackPlayed = true;
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _wasHitAttack = false;
            _elapsedTime = 0;

            // OnStateExit はクリップの再生終了時ではなく、遷移の開始時に呼ばれる。
            // ここで通知すると、遷移ブレンド中に BehaviourTree が次の攻撃を開始してしまう。
            NotifyAttackEnd();

            _animationEventReceiver.OnHitAttack -= HandleAttackHit;
        }

        public void StopPlayAttack()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public void SetAttackData(Attack.AttackData attackData)
        {
            _attackData = attackData;
        }

        [Header("攻撃ID")]
        [SerializeField] protected int _attackID = 0;

        [Header("攻撃開始時間")]
        [SerializeField] protected float _attackStartTime = 0.5f;

        [Header("攻撃範囲が生成されてから消えるまでの時間")]
        [SerializeField] protected float _attackAreaDespawnTime = 1.0f;

        [Header("攻撃時のCameraShakeData")]
        [SerializeField] protected CameraShakeData _cameraShakeData;

        // 攻撃対象
        protected IPlayer _attackTarget;

        // 攻撃命中済みフラグ
        protected bool _wasHitAttack = false;

        // 攻撃開始済み開始フラグ
        protected bool _isAttackPlayed = false;

        // 範囲表示と実判定で共有する、現在の攻撃範囲の中心座標。
        protected Vector3 _attackAreaCenter;

        // 音源CueName
        protected virtual string AttackStartVoiceCueName => null;

        protected virtual string AttackCueName => null;

        // カメラマネージャー
        protected CameraManager _cameraManager = null;

        // エフェクト再生関連
        protected EffectManager _effectManager = null;
        protected IAttackHitAreaSpawner _attackHitAreaSpawner = null;

        // 攻撃データ
        protected Attack.AttackData _attackData = default;

        // 現在の再生開始からの経過時間（秒）
        protected float _elapsedTime;

        // 攻撃が中断になった際のCancellationTokenSource
        private CancellationTokenSource _cts = new();

        protected void PlayBossSE(string cueName)
        {
            if (string.IsNullOrEmpty(cueName)) return;
            if (_bossCharacterTransform == null) return;

            Sound.PlaySE(_bossCharacterTransform.gameObject, cueName, CueSheetType.Boss);
        }

        protected virtual HitAreaView VisibleAttackArea(AttackHitAreaType attackHitAreaType, Vector3 position)
        {
            HitAreaView hitArea
                = _attackHitAreaSpawner.Spawn(
                    attackHitAreaType, 
                    position, 
                    _attackData.AttackHitAreaRadius, 
                    _attackAreaDespawnTime);

            return hitArea;
        }

        protected virtual void HandleAttackHit() => _wasHitAttack = true;

        protected async virtual UniTask PlayAttack(CancellationToken cancellationToken) { }

        /// <summary>
        /// 攻撃固有の演出・判定が完了したら Animator の攻撃パラメータを解除する。
        /// 攻撃終了イベントそのものは Idle への遷移完了後に送るため、
        /// Animator の遷移条件と攻撃終了通知が相互待ちにならない。
        /// </summary>
        private async UniTaskVoid PlayAttackAsync(CancellationToken cancellationToken)
        {
            try
            {
                await PlayAttack(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 攻撃中止
            }
        }

        /// <summary>
        /// 攻撃ステートから Idle への遷移が完了してから攻撃終了を通知する。
        /// 他の攻撃ステートへの割り込み遷移では、古い攻撃の終了通知を送らない。
        /// </summary>
        private void NotifyAttackEnd()
        {
            if (_animationEventReceiver == null) return;

            _animationEventReceiver.AnimEvent_AttackCompleted();

            Debug.Log("攻撃終了");
        }

        private void OnDestroy()
        {
            // 破棄時に非同期処理をキャンセル
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
