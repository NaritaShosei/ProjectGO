using System;
using UniRx;
using UnityEngine;

namespace BossEnemy.Armor
{
    /// <summary> BossEnemyが装着するArmerの実体 </summary>
    public struct ArmorStatus
    {
        public ArmorStatus(int maxHP, int defense)
        {
            _maxHP = maxHP;
            _defense = defense;
            _isArmorBroken = false;
        }

        /// <summary> 最大HP </summary>
        public int MaxHP => _maxHP; 

        /// <summary> 防御力 </summary>
        public int Defense => _defense;

        /// <summary> アーマー破壊フラグ </summary>
        public bool IsArmorBroken => _isArmorBroken;

        /// <summary> 初期化 </summary>
        public void Init()
        {
            _isArmorBroken = false;
        }

        public void Break() => _isArmorBroken = true;

        /// <summary> Armerの修復処理 </summary>
        public void Repair()
        {
            // 破壊されていなければ元に戻す
            if (!_isArmorBroken) return;

            // 破壊済み判定をFalseに
            _isArmorBroken = false;
        }

        // 最大HP
        private int _maxHP;

        // 防御力
        private int _defense;

        // アーマーのHPが0になって壊れた際にTrueになるフラグ
        private bool _isArmorBroken;
    }
}
