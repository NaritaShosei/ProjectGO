using BossEnemy.Effect;
using BossEnemy.Interface;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace BossEnemy.SMB
{
    public abstract class AttackSMB : BossCharacterSMB, IUpdater
    {
        private const float ANIM_END_NORMALIZED_TIME = 1.0f;

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
            // 同じSMBが前回の非同期攻撃の完了前に再入した場合、前回の処理は
            // 現在の経過時間・判定情報を上書きしてしまう。そのため、先に実行世代を
            // 更新して前回の後始末を無効化してから、実行中の処理を中断する。
            _attackExecutionVersion++;
            CancelActivePlayAttacks();

            // アニメーションステート開始フラグ
            _isStopPlayAttack = false;
            _isAttackAnimationEndRequested = false;
            _isAttackCompletionNotified = false;
            _isAttackCompleted = false;

            // 早期終了時も含め、前回攻撃の判定状態を持ち越さない。
            ResetAttackHitCheckState();

            // 経過時間をリセット
            _elapsedTime = 0;

            // 攻撃開始済みフラグ
            _isAttackPlayed = false;

            // 攻撃発動時間がAnimationの時間より長い場合攻撃を取りやめる
            if ( _attackStartTime >= stateInfo.length)
            {
                _isAttackCompleted = true;
                NotifyAttackAnimCompleted();
                return;
            }

            // 攻撃ごとにトークンを保持する。通常のアニメーション終了では
            // 既存の PlayAttack を中断しない。
            _currentPlayAttackCts = new CancellationTokenSource();
            _playAttackCancellationTokenSources.Add(_currentPlayAttackCts);

            // 攻撃ヒットイベントの発火を検知できるようにする
            _animationEventReceiver.OnHitAttack += HandleAttackHit;
        }

        /// <summary> 毎フレーム行われる処理 </summary>
        public void OnUpdate()
        {
            // 攻撃が終了済みなら何もしない
            if (_isAttackCompleted) return;

            // 攻撃時間の計測
            _elapsedTime += Time.deltaTime * _timeScale;

            // 攻撃の当たり判定フラグがTrueなら当たり判定イベントを発火する
            if (_isAttackHitCheck)
            {
                // 判定回数を1フレームごとに増加
                _currentHitCheckCount++;

                if (!_wasHitAttack)
                {
                    // 攻撃の当たり判定イベントの発火
                    _animationEventReceiver.AnimEvent_AttackHitCheck
                        (_attackData, _attackHitCollisionType, _attackCenterPos);
                }

                // もし攻撃の当たり判定を行うフレーム数が設定されていなければ
                // 当たり判定情報の初期化を行わない
                if (_attackHitCheckFrameCount == 0) return;

                // 最後の判定を終えたら攻撃の当たり判定情報の初期化を行う
                if (_currentHitCheckCount >= _attackHitCheckFrameCount)
                {
                    _isAttackHitCheck = false;
                    _wasHitAttack = false;
                    _currentHitCheckCount = 0;
                }
            }
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 攻撃開始時間になったら攻撃を始める
            if(_elapsedTime >= _attackStartTime && !_isAttackPlayed && _currentPlayAttackCts != null)
            {
                // 攻撃を開始する
                PlayAttackAsync(_currentPlayAttackCts, _attackExecutionVersion).Forget();

                // 攻撃開始済みフラグをTrueにする
                _isAttackPlayed = true;

                // 攻撃が開始されたので攻撃終了フラグをFalseにする
                _isAttackCompleted = false;
            }

            // StateのAnimationがすべて再生されていれば攻撃を終了する
            if (!_isAttackAnimationEndRequested
                && stateInfo.normalizedTime >= ANIM_END_NORMALIZED_TIME)
            {
                _isAttackAnimationEndRequested = true;
                _bossCharacterView.FinishAttackAnimation();
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Stateを抜けた段階でAnimatorの攻撃解除が間に合っていなければすぐに解除する
            if (!_isAttackAnimationEndRequested)
            {
                _isAttackAnimationEndRequested = true;
                _bossCharacterView.FinishAttackAnimation();
            }

            // 攻撃が始まる前にStateを抜けた場合攻撃のために用意したCancellationTokenSourcesを削除する
            if (_elapsedTime < _attackStartTime)
            {
                // OnStateEnter の早期終了時、および StopPlayAttack 実行後は
                // TokenSource が生成済みでない（または既に解放済み）ことがある。
                // OnStateExit はその場合にも呼ばれるため、存在する場合だけ解放する。
                CancellationTokenSource currentPlayAttackCts = _currentPlayAttackCts;
                if (currentPlayAttackCts != null)
                {
                    _playAttackCancellationTokenSources.Remove(currentPlayAttackCts);
                    currentPlayAttackCts.Dispose();
                    _currentPlayAttackCts = null;
                }
            }

            _wasHitAttack = false;

            // 途中で攻撃が中断されていなければ、攻撃ステートを抜けた時点で終了通知を飛ばす。
            // 遷移先ステートは直後に別ステートへ遷移し得るため、ここで判定しない。
            if (!_isStopPlayAttack)
            {
                _isStopPlayAttack = true;
                NotifyAttackAnimCompleted();
            }

            // イベント購読解除
            _animationEventReceiver.OnHitAttack -= HandleAttackHit;
        }

        /// <summary> 攻撃中断メソッド </summary>
        public void StopPlayAttack()
        {
            // 以後に実行される finally がこの攻撃の状態を変更しないようにする。
            _attackExecutionVersion++;
            CancelActivePlayAttacks();
            ResetAttackHitCheckState();
            _isAttackCompleted = true;

            // 攻撃中断フラグをTrueに
            _isStopPlayAttack = true;
        }

        /// <summary>
        /// プール返却時に進行中の攻撃と購読を確実に終了する。
        /// </summary>
        public override void Dispose()
        {
            StopPlayAttack();
            if (_animationEventReceiver != null)
                _animationEventReceiver.OnHitAttack -= HandleAttackHit;

            base.Dispose();
        }

        /// <summary> 攻撃データを設定 </summary>
        public virtual void SetAttackData(Attack.AttackData attackData)
        {
            _attackData = attackData;
        }

        [Header("攻撃ID")]
        [SerializeField] protected int _attackID = 0;

        [Header("攻撃開始時間")]
        [SerializeField] protected float _attackStartTime = 0.5f;

        [Header("攻撃範囲が生成されてから攻撃が行われるまでの時間")]
        [SerializeField] protected float _attackAreaDespawnAndDisplayAttackEffectTime = 1.0f;

        [Header("1回の攻撃で当たり判定を行うフレーム数")]
        [SerializeField] protected int _attackHitCheckFrameCount = 7;

        [Header("攻撃の当たり判定の形")]
        [SerializeField] protected AttackHitAreaType _attackHitCollisionType = AttackHitAreaType.Circle;

        [Header("攻撃時のCameraの振動の強さデータ")]
        [SerializeField] protected CameraShakeData _cameraShakeData;

        // 攻撃対象
        protected IPlayer _attackTarget;

        // 攻撃の当たり判定を行った回数
        protected int _currentHitCheckCount = 0;

        // 攻撃命中済みフラグ
        protected bool _wasHitAttack = false;

        // 攻撃の当たり判定を行うフラグ
        protected bool _isAttackHitCheck = false;

        // 攻撃開始済みフラグ
        protected bool _isAttackPlayed = false;

        // 攻撃終了フラグ
        protected bool _isAttackCompleted = true;

        // 範囲表示と実判定で共有する、現在の攻撃範囲の中心座標。
        protected Vector3 _attackCenterPos = Vector3.zero;

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

        // 実行中の攻撃ヒットエリアのList
        protected readonly List<HitAreaView> _visibleHitAreaList = new();

        // 実行中のPlayAttackをアニメーションStateとは独立して追跡するCancellationTokenSource
        private readonly List<CancellationTokenSource> _playAttackCancellationTokenSources = new();
        private CancellationTokenSource _currentPlayAttackCts;

        // 攻撃中断フラグ
        private bool _isStopPlayAttack = false;

        // 同じ攻撃ステートの早期終了と OnStateExit による二重通知を防ぐ。
        private bool _isAttackCompletionNotified;

        // 攻撃クリップ終端でAnimatorの攻撃フラグを解除済みか。
        private bool _isAttackAnimationEndRequested;

        // 同じSMBが再入した際に、古い非同期処理の後始末を識別する実行世代番号。
        private int _attackExecutionVersion;

        // 攻撃が当たった際のイベント発火時の処理
        protected virtual void HandleAttackHit() => _wasHitAttack = true;

        // 攻撃処理
        protected async virtual UniTask PlayAttack(CancellationToken cancellationToken) { }

        /// <summary> 攻撃の当たり判定開始処理 </summary>
        protected virtual void StartAttackHitCheck(Vector3 attackCenterPos)
        {
            _attackCenterPos = attackCenterPos;
            _wasHitAttack = false;
            _isAttackHitCheck = true;
            _currentHitCheckCount = 0;
        }

        /// <summary>
        /// 攻撃固有の演出・判定が完了したら Animator の攻撃パラメータを解除する。
        /// 攻撃終了イベントそのものは Idle への遷移完了後に送るため、
        /// Animator の遷移条件と攻撃終了通知が相互待ちにならない。
        /// </summary>
        private async UniTaskVoid PlayAttackAsync(CancellationTokenSource cts, int executionVersion)
        {
            try
            {
                // 攻撃開始
                await PlayAttack(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // 攻撃中止
            }
            finally
            {
                // 同じSMBが再入済みなら、この古い処理は現行攻撃の状態に触れない。
                if (executionVersion == _attackExecutionVersion)
                {
                    // 攻撃が完全終了したので攻撃終了フラグをTrueにする
                    _isAttackCompleted = true;

                    // 実行中の攻撃範囲が残っていれば見えないようにする
                    if (_visibleHitAreaList.Count > 0)
                    {
                        foreach (var hitArea in _visibleHitAreaList)
                        {
                            hitArea.InVisible();
                        }

                        _visibleHitAreaList.Clear();
                    }
                }

                // 攻撃の完全終了とみなしCancellationTokenSourcesを開放
                _playAttackCancellationTokenSources.Remove(cts);
                cts.Dispose(); 
            }
        }

        /// <summary> 前回実行中の攻撃を中断し、残った範囲表示を回収する </summary>
        private void CancelActivePlayAttacks()
        {
            foreach (CancellationTokenSource cts in _playAttackCancellationTokenSources.ToArray())
            {
                cts.Cancel();
                cts.Dispose();
            }

            _playAttackCancellationTokenSources.Clear();
            _currentPlayAttackCts = null;

            foreach (HitAreaView hitArea in _visibleHitAreaList.ToArray())
            {
                hitArea.InVisible();
            }

            _visibleHitAreaList.Clear();
        }

        /// <summary> 攻撃判定に関する共有状態を初期化する </summary>
        private void ResetAttackHitCheckState()
        {
            _wasHitAttack = false;
            _isAttackHitCheck = false;
            _currentHitCheckCount = 0;
            _attackCenterPos = Vector3.zero;
        }

        /// <summary> 攻撃アニメーション終了通知 </summary>
        private void NotifyAttackAnimCompleted()
        {
            if (_isAttackCompletionNotified) return;
            _isAttackCompletionNotified = true;

            if (_animationEventReceiver == null) return;

            _animationEventReceiver.AnimEvent_AttackCompleted();
            Debug.Log("攻撃の完全終了");
        }

        private void OnDestroy()
        {
            // 破棄時に非同期処理をキャンセル
            StopPlayAttack();
        }
    }
}
