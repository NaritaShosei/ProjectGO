using System;
using UnityEngine;

namespace BossEnemy.Attack
{
    /// <summary> ボスエネミーの攻撃情報 </summary>
    public struct AttackSelectionPool
    {
        [Serializable]
        public struct AttackCondition
        {
            [Header("攻撃DataのID")]
            public int ID;

            [Header("攻撃の発動確率")]
            public int ActivationRate;
        }

        public AttackCondition[] SelectionPool => _attackField;

        [SerializeField, Tooltip("ボスエネミーの攻撃マスターデータ")]
        private AttackCondition[] _attackField;
    }
}
