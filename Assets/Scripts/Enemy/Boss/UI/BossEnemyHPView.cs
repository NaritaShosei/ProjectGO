using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

using BossEnemy.Character;
using BossEnemy.Interface;

namespace BossEnemy.UI
{
    public class BossEnemyHPView : MonoBehaviour, IBossHPView, IPoolable
    {
        #region BossEnemyのHPBarClass
        [Serializable]
        public class HPBarUI
        {
            /// <summary> 初期化 </summary>
            /// <param name="maxHP"> 最大HP </param>
            public void Init(int maxHP)
            {
                _maxHP = maxHP;
                _takeDamageSequence = DOTween.Sequence();
                _currentHPBar.gameObject.SetActive(true);
                _damageBar.gameObject.SetActive(true);

                // HPBarを最大にする
                int fillAmountMaxValue = 1;
                _currentHPBar.fillAmount = fillAmountMaxValue;
                _damageBar.fillAmount = fillAmountMaxValue;
            }

            /// <summary> 使い終わった際の処理 </summary>
            public void Disable()
            {
                _currentHPBar.gameObject.SetActive(false);
                _damageBar.gameObject.SetActive(false);
                _takeDamageSequence?.Kill();
                _takeDamageSequence = null;
            }

            /// <summary> ダメージを受けた際の処理 </summary>
            public async UniTask TakeDamage(int currentHP)
            {
                _takeDamageSequence?.Kill();
                _takeDamageSequence = DOTween.Sequence();

                float endValue = (float)currentHP / (float)_maxHP;

                await _takeDamageSequence.Append(_currentHPBar.DOFillAmount(endValue, _takeDamageAnimDuration));
                await UniTask.Delay(_finishDamageDuration);
                await _takeDamageSequence.Append(_damageBar.DOFillAmount(endValue, _takeDamageAnimDuration));
            }

            [Header("現在のHPを表すUI")]
            [SerializeField] private Image _currentHPBar;

            [Header("ダメージを受けた際の総量を表すUI")]
            [SerializeField] private Image _damageBar;

            [Header("ダメージを受けた際のHP減少を表現の時間設定")]
            [SerializeField, Tooltip("現在のHPを減少させるまでの時間")]
            private float _takeDamageAnimDuration;

            [SerializeField, Tooltip("HPBarのダメージ表現を終了させるまでの時間")]
            private int _finishDamageDuration;

            private int _maxHP;

            private Sequence _takeDamageSequence = null;
        }
        #endregion

        public HPBarUI CurrentBar => _currentHPBar;

        public void OnGet() { }

        public void OnRelease()
        {
            _disposable?.Dispose();
        }

        /// <summary> 次のPhaseのHPBarに切り替える処理 </summary>
        public void PhaseChange(Status bossEnemyData, int currentPhase)
        {
            if (currentPhase >= _bossEnemyAllPhaseHPBarArray.Length)
            {
                Debug.LogError("存在しないPhaseのHPBarが選ばれました");
                return;
            }

            _disposable?.Dispose();
            _disposable = new();

            // 現在使用中のHPBarがあれば破棄
            _currentHPBar?.Disable();

            _currentHPBar = _bossEnemyAllPhaseHPBarArray[currentPhase];
            _currentHPBar.Init(bossEnemyData.MaxHP);

            bossEnemyData.CurrentHP.Subscribe(async hp =>
            {
                await CurrentBar.TakeDamage(hp);
            }).AddTo(_disposable);
        }

        [Header("各PhaseでのボスエネミーのHPUI")]
        [SerializeField] private HPBarUI[] _bossEnemyAllPhaseHPBarArray;

        private HPBarUI _currentHPBar = null;

        private CompositeDisposable _disposable;

        private void OnDestroy()
        {
            _disposable?.Dispose();
        }
    }

}
