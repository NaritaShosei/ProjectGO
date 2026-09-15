using UnityEngine;

namespace BossEnemy.Interface
{
    public interface ICanMoveAreaChecker 
    {
        /// <summary> 移動可能判定 </summary>
        public bool CanMove(Vector3 movePos);
    }
}
