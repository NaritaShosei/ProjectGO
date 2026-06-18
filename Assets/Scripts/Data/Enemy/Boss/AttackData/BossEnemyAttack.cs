using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BossEnemy.Data
{
    public enum AttackHitAreaType
    {
        [InspectorName("円形")] Circle,
        [InspectorName("扇形")] FanShape,
        [InspectorName("長方形")] Rectangle
    }

    public interface IAttack
    {
        public UniTask OnAttack();
    }
    
    public abstract class BossEnemyAttack : ScriptableObject, IAttack
    {
        [SerializeField, Tooltip("攻撃力")]
        private float _damage = 10;

        [SerializeField, Tooltip("攻撃の名称")]
        private string _attackName = "パンチ";

        [SerializeField, Tooltip("アニメーションを流すためのパラメータの名称")]
        private string _animParamName = "paramName";

        public abstract UniTask OnAttack();
    }
}
