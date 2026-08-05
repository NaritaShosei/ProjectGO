using UnityEngine;

namespace BossEnemy.Interface
{
    public interface IBossHPUIPresenter
    {
        /// <summary> ボス戦開始イベント発火時の処理 </summary>
        public void HandleBossSpawn();

        /// <summary> ボス戦終了イベント発火時の処理 </summary>
        public void HandleBossDead();

        /// <summary> 被ダメージイベント発火時の処理 </summary>
        public void HandleTakeDamage(int damage);

        /// <summary> フェーズ切り替えイベント発火時の処理 </summary>
        protected void HandlePhaseChange();
    }
}
