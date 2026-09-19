using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using BossEnemy.Character;
using BossEnemy.Enum;
using System.Threading;

namespace BossEnemy.Armor
{
    /// <summary> ボスの装備するアーマー </summary>
    public class BossArmorView : MonoBehaviour, IArmorHealth, IDisposable
    {
        public ArmorAttachmentType AttachmentPoints => _armorAttachmentPointsType;

        public bool IsBroken => _isBreak;

        /// <summary> アーマーの現在HP（Entity未設定時は破壊済み0・健在1の2値） </summary>
        public float CurrentHealth => _entity?.ArmorCurrentHPDict != null && _entity.ArmorCurrentHPDict.TryGetValue(AttachmentPoints, out int hp)
            ? hp
            : (_isBreak ? 0f : 1f);

        /// <summary> アーマーの最大HP </summary>
        public float MaxHealth => _entity?.ArmorCurrentHPDict != null ? _entity.GetArmorStats(AttachmentPoints).MaxHP : 1f;

        /// <summary> HP変化時に発火するイベント </summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary> 破壊時に発火するイベント </summary>
        public event Action OnBroken;

        /// <summary> 修復時に発火するイベント </summary>
        public event Action OnRepaired;

        public void Init()
        {
            RepairArmor().Forget();
        }

        public void Dispose()
        {
            _repairCancellationTokenSource?.Cancel();
            _repairCancellationTokenSource?.Dispose();
            _repairCancellationTokenSource = null;
        }

        /// <summary> 実際の耐久値を持つEntityを登録する（未登録なら2値表示にフォールバック） </summary>
        public void SetEntity(IBossCharacterEntity entity)
        {
            _entity = entity;
            _hasSyncedInitialHP = false;
            Debug.Log($"[BossArmorView] SetEntity: {gameObject.name} / {AttachmentPoints}");
        }

        /// <summary> アーマーのワールド座標を取得する（未設定ならこのオブジェクト自身の位置） </summary>
        public Transform GetTargetCenter() => _gaugeAnchor != null ? _gaugeAnchor : transform;

        /// <summary> アーマー修復時の処理 </summary>
        public async UniTaskVoid RepairArmor()
        {
            if (_isBreak == false) return;

            _repairCancellationTokenSource?.Cancel();
            _repairCancellationTokenSource?.Dispose();
            var repairCancellationTokenSource = new CancellationTokenSource();
            _repairCancellationTokenSource = repairCancellationTokenSource;

            try
            {
                await UniTask.Delay(
                TimeSpan.FromSeconds(_repairDelayTime),
                cancellationToken: repairCancellationTokenSource.Token);

                this.gameObject.SetActive(true);
                _isBreak = false;

                Debug.Log($"[BossArmorView] RepairArmor: {gameObject.name} / {AttachmentPoints}");

                // 修復をUI等の購読者に通知
                OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
                OnRepaired?.Invoke();
            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                // 後から始まった修復のCTSを、古い処理のfinallyで破棄しない。
                if (_repairCancellationTokenSource == repairCancellationTokenSource)
                {
                    repairCancellationTokenSource.Dispose();
                    _repairCancellationTokenSource = null;
                }
            }
        }

        /// <summary> アーマー破壊時の処理 </summary>
        public async UniTaskVoid BreakArmor()
        {
            if (_isBreak == true) return;

            _repairCancellationTokenSource?.Cancel();
            _repairCancellationTokenSource?.Dispose();
            _repairCancellationTokenSource = null;

            this.gameObject.SetActive(false);
            _isBreak = true;

            Debug.Log($"[BossArmorView] BreakArmor: {gameObject.name} / {AttachmentPoints}");

            // 破壊をUI等の購読者に通知
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnBroken?.Invoke();
        }

        [Header("Armorの装着ヶ所を示すEnum")]
        [SerializeField, Tooltip("Armorの装着ヶ所を示すEnum")]
        private ArmorAttachmentType _armorAttachmentPointsType;

        [Header("ゲージ表示位置（未設定ならこのオブジェクトの位置を使用）")]
        [SerializeField, Tooltip("ゲージ表示位置（未設定ならこのオブジェクトの位置を使用）")]
        private Transform _gaugeAnchor;

        [Header("アーマーの修理終了までの時間")]
        [SerializeField] private float _repairDelayTime = 1;

        private bool _isBreak = false;

        private IBossCharacterEntity _entity;

        private int _lastKnownHP = int.MinValue;

        private bool _hasSyncedInitialHP = false;

        private CancellationTokenSource _repairCancellationTokenSource = null;

        /// <summary> Entity側は1発ごとのダメージで通知を出さないため、ここでHP変化を検知する </summary>
        private void Update()
        {
            if (_entity?.ArmorCurrentHPDict == null) return;
            if (!_entity.ArmorCurrentHPDict.TryGetValue(AttachmentPoints, out int hp)) return;

            // ArmorCurrentHPDictがまだ未初期化だった間は基準値を持てないため、
            // 実際に値が読めた最初の1回は「変化」として通知せず、基準値の同期だけ行う。
            if (!_hasSyncedInitialHP)
            {
                _hasSyncedInitialHP = true;
                _lastKnownHP = hp;
                return;
            }

            if (hp == _lastKnownHP) return;

            _lastKnownHP = hp;
            Debug.Log($"[BossArmorView] HP変化検知: {gameObject.name} / {AttachmentPoints} / HP:{hp}");
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
    }
}

