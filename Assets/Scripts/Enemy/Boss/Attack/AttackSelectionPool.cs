using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BossEnemy.Attack
{
    [Serializable]
    public struct AttackCondition
    {
        [Header("攻撃DataのID")]
        public int ID;

        [Header("攻撃の発動確率")]
        public int ActivationRate;
    }

    [Serializable]
    /// <summary> ボスエネミーの攻撃情報 </summary>
    public struct AttackSelectionPool
    {
        public void SetSelectionPool(AttackCondition[] selectionField) => _attackField = selectionField;

        public AttackCondition[] SelectionPool => _attackField;

        private AttackCondition[] _attackField;
    }
}
