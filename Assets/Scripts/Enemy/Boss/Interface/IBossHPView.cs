using BossEnemy.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BossEnemy.Interface
{
    public interface IBossHPView 
    {
        // HPが0になったときのフラグ
        public bool IsHPZero { get; }

        /// <summary> 初期化 </summary>
        public void Init(BossCharacterHPUIPresenter presenter);

        /// <summary> 次のPhaseのHPBarに切り替える処理 </summary>
        public UniTaskVoid ChangeHPUI(int maxHP, int currentPhase);

        /// <summary> ダメージを受けてHPが減少する際の処理 </summary>
        public UniTask TakeDamage(int currentHP);

        /// <summary> 処理中のタスクを設定 </summary>
        public void SetRunningTask(UniTask runningTask);
    }
}
