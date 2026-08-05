using Cysharp.Threading.Tasks;
using UnityEngine;

#region BossEnemy関連
using BossEnemy.Enum;
#endregion


namespace BossEnemy.Armor
{
    /// <summary> ボスの装備するアーマー </summary>
    public class BossArmorView : MonoBehaviour
    {
        public ArmorAttachmentType AttachmentPoints => _armorAttachmentPointsType;

        public bool IsBreak => _isBreak;

        public void Init()
        {
            RepairArmor().Forget();
        }

        /// <summary> アーマー修復時の処理 </summary>
        public async UniTask RepairArmor()
        {
            if (_isBreak == false) return;



            this.gameObject.SetActive(true);
            _isBreak = false;
        }

        /// <summary> アーマー破壊時の処理 </summary>
        public async UniTask BreakArmer()
        {
            if (_isBreak == true) return;



            this.gameObject.SetActive(false);
            _isBreak = true;
        }

        [Header("Armorの装着ヶ所を示すEnum")]
        [SerializeField, Tooltip("Armorの装着ヶ所を示すEnum")]
        private ArmorAttachmentType _armorAttachmentPointsType;

        private bool _isBreak = false;

        private EffectManager _effectManager;

        private void Awake()
        {
            _effectManager = FindFirstObjectByType<EffectManager>();
        }
    }
}

