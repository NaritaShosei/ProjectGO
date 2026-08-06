using System;
using UniRx;
using UnityEngine;

namespace BossEnemy.Armor
{
    /// <summary> BossEnemyが装着するArmerの実体 </summary>
    public struct ArmorStatus
    {
        /// <summary> 現在のHP </summary>
        public IReadOnlyReactiveProperty<int> CurrentHP => _currentHP;

        /// <summary> 防御力 </summary>
        public int Defense => _defense;

        /// <summary> アーマー破壊フラグ </summary>
        public bool IsArmorBreak => _isArmorBroken;

        /// <summary> Armerの修復処理 </summary>
        /// <param name="repairedArmorHP"> 修復後のHP </param>
        public void Repair(int repairedArmorHP)
        {
            // 破壊されていなければ元に戻す
            if (!_isArmorBroken) return;

            // もし修復後のHPが最大HPより大きいか0よりも小さければ修復後のHPを自動的に最大HPにする
            if (repairedArmorHP <= 0 || repairedArmorHP >= _maxHP)
                _currentHP.Value = _maxHP;
            // そうでなければrepairedArmorHPの値を反映
            else _currentHP.Value = repairedArmorHP;

            // 破壊済み判定をFalseに
            _isArmorBroken = false;
        }

        /// <summary> Armerへのダメージメソッド </summary>
        /// <param name="damage"> ダメージ総量 </param>
        public void TakeDamage(int damage)
        {
            if (_currentHP.Value - damage <= 0)
            {
                _isArmorBroken = true;
                _currentHP.Value = 0;
                return;
            }

            _currentHP.Value -= damage;
        }

        // 最大HP
        private int _maxHP;

        // 現在のHP
        private ReactiveProperty<int> _currentHP;

        // 防御力
        private int _defense;

        // アーマーのHPが0になって壊れた際にTrueになるフラグ
        private bool _isArmorBroken;
    }
}
