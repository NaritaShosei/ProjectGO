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
    public class BossCharacterHPUIView : MonoBehaviour, IBossHPView, IPoolable
    {
        public event Action OnChangeHPBarCompleted;

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

                float endValue = (float)currentHP / (float)_maxHP;

                _takeDamageSequence = DOTween.Sequence()
                    .Append(_currentHPBar.DOFillAmount(endValue, _takeDamageAnimDuration))
                    .AppendInterval(_finishDamageDuration)
                    .Append(_damageBar.DOFillAmount(endValue, _takeDamageAnimDuration));
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

        public bool IsHPZero => _isHPZero;

        public void OnGet() { }

        public void OnRelease()
        {
            _presenter.Dispose();
        }

        /// <summary> 初期化 </summary>
        public void Init(BossCharacterHPUIPresenter presenter)
        {
            _presenter = presenter;
            _isHPZero = false;
        }

        /// <summary> 次のPhaseのHPBarに切り替える処理 </summary>
        public async UniTaskVoid ChangeHPBar(int maxHP, int currentPhase)
        {
            _isHPZero = false;

            int nextHPBarArrNum = currentPhase - 1;
            if (nextHPBarArrNum >= _bossEnemyAllPhaseHPBarArray.Length)
            {
                Debug.LogError("存在しないPhaseのHPBarが選ばれました");
                return;
            }

            await _runningTask;

            // 現在使用中のHPBarがあれば破棄
            _currentHPBar?.Disable();

            _currentHPBar = _bossEnemyAllPhaseHPBarArray[nextHPBarArrNum];
            _currentHPBar.Init(maxHP);

            OnChangeHPBarCompleted?.Invoke();
            Debug.Log("HPUIの設定が完了しました");
        }
        
        public async UniTask TakeDamage(int currentHP)
        {
            if (_currentHPBar == null || _isHPZero) return;

            if (currentHP <= 0)
            {
                Debug.Log("HPが0になりました");
                _isHPZero = true;
            }

            Debug.Log($"HP減少 現在のHP: {currentHP}");
            await _currentHPBar.TakeDamage(currentHP);
        }

        public void SetRunningTask(UniTask runningTask)
        {
            _runningTask = runningTask;
        }

        [Header("各PhaseでのボスエネミーのHPUI")]
        [SerializeField] private HPBarUI[] _bossEnemyAllPhaseHPBarArray;

        private HPBarUI _currentHPBar = null;

        private BossCharacterHPUIPresenter _presenter;

        private UniTask _runningTask = UniTask.CompletedTask;

        private bool _isHPZero = false;
    }

}
