using BossEnemy.Effect;
using BossEnemy.Interface;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace BossEnemy.SMB
{
    public abstract class AttackSMB : BossCharacterSMB
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
            _animationStateVersion++;

            Debug.Log($"[AttackSMB] Enter: id={_attackID}, version={_animationStateVersion}, layer={layerIndex}");

            // アニメーションステート開始フラグ
            _isStopPlayAttack = false;
            _isAttackAnimationEndRequested = false;
            _isAttackCompletionNotified = false;

            // 攻撃発動時間がAnimationの時間より長い場合攻撃を取りやめる
            if ( _attackStartTime >= stateInfo.length)
            {
                NotifyAttackAnimCompleted();
                return;
            }

            // 経過時間をリセット
            _elapsedTime = 0;

            // 攻撃命中済みフラグをfalseに
            _wasHitAttack = false;

            // 攻撃開始済みフラグ
            _isAttackPlayed = false;

            // 攻撃ごとにトークンを保持する。通常のアニメーション終了では
            // 既存の PlayAttack を中断しない。
            _currentPlayAttackCts = new CancellationTokenSource();
            _playAttackCancellationTokenSources.Add(_currentPlayAttackCts);

            // 攻撃ヒットイベントの発火を検知できるようにする
            _animationEventReceiver.OnHitAttack += HandleAttackHit;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 攻撃時間の計測
            _elapsedTime += Time.deltaTime * _timeScale;

            // 攻撃開始時間になったら攻撃を始める
            if(_elapsedTime >= _attackStartTime && !_isAttackPlayed && _currentPlayAttackCts != null)
            {
                // 攻撃を開始する
                PlayAttackAsync(_currentPlayAttackCts).Forget();

                // 攻撃開始済みフラグをTrueにする
                _isAttackPlayed = true;
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
            Debug.Log($"[AttackSMB] Exit: id={_attackID}, version={_animationStateVersion}, layer={layerIndex}");

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
            // すべての攻撃を強制的に終了させる
            foreach (CancellationTokenSource cts in _playAttackCancellationTokenSources.ToArray())
            {
                cts.Cancel();
                cts.Dispose();
            }

            _playAttackCancellationTokenSources.Clear();
            _currentPlayAttackCts = null;

            // 攻撃中断フラグをTrieに
            _isStopPlayAttack = true;
        }

        /// <summary> 攻撃データを設定 </summary>
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

        // 実行中のPlayAttackをアニメーションStateとは独立して追跡するCancellationTokenSource
        private readonly List<CancellationTokenSource> _playAttackCancellationTokenSources = new();
        private CancellationTokenSource _currentPlayAttackCts;

        // 攻撃中断フラグ
        private bool _isStopPlayAttack = false;
        private uint _animationStateVersion;

        // 同じ攻撃ステートの早期終了と OnStateExit による二重通知を防ぐ。
        private bool _isAttackCompletionNotified;

        // 攻撃クリップ終端でAnimatorの攻撃フラグを解除済みか。
        private bool _isAttackAnimationEndRequested;

        // ボスの音源再生メソッド
        protected void PlayBossSE(string cueName)
        {
            if (string.IsNullOrEmpty(cueName)) return;
            if (_bossCharacterTransform == null) return;

            // 再生
            Sound.PlaySE(_bossCharacterTransform.gameObject, cueName, CueSheetType.Boss);
        }

        protected virtual void HandleAttackHit() => _wasHitAttack = true;

        protected async virtual UniTask PlayAttack(CancellationToken cancellationToken) { }

        /// <summary>
        /// 攻撃固有の演出・判定が完了したら Animator の攻撃パラメータを解除する。
        /// 攻撃終了イベントそのものは Idle への遷移完了後に送るため、
        /// Animator の遷移条件と攻撃終了通知が相互待ちにならない。
        /// </summary>
        private async UniTaskVoid PlayAttackAsync(CancellationTokenSource cts)
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
                // 攻撃の完全終了とみなしCancellationTokenSourcesを開放
                _playAttackCancellationTokenSources.Remove(cts);
                cts.Dispose(); 
            }
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
