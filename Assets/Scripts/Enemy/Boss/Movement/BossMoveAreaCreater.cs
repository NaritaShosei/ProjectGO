using UnityEngine;
using BossEnemy.Interface;

namespace BossEnemy.Movement
{
    /// <summary> ボスの移動範囲を作るクラス </summary>
    public class BossMoveAreaCreater : MonoBehaviour, ICanMoveAreaChecker
    {
        /// <summary> 移動可能判定 </summary>
        public bool CanMove(Vector3 movePos, Vector3 currentPos, out Vector3 newMovePos)
        {
            newMovePos = currentPos;

            if(_center == null)
            {
                Debug.LogError("中心地点が指定されていません");
                return false;
            }

            movePos.y = currentPos.y;
            var center = new Vector3()
            {
                x = _center.position.x,
                y = currentPos.y,
                z = _center.position.z
            };

            float movePosDistance = Vector3.Distance(center, movePos);

            // 半径のほうが移動場所より中心地点から遠ければ移動可能
            if (_radius >= movePosDistance) return true;

            newMovePos.x = movePos.x; 
            movePosDistance = Vector3.Distance(center, newMovePos);

            // 半径のほうが移動場所より中心地点から遠ければ移動可能
            if (_radius >= movePosDistance) return false;

            newMovePos.x = currentPos.x;
            newMovePos.z = movePos.z;
            movePosDistance = Vector3.Distance(center, newMovePos);

            // 半径のほうが移動場所より中心地点から遠ければ移動可能
            if (_radius >= movePosDistance) return false;

            newMovePos = currentPos;
            return false;
        }

        [Header("動ける範囲の中心地点")]
        [SerializeField] private Transform _center;

        [Header("中心地点から円形を描く際実際に動ける範囲の大きさを決める半径")]
        [SerializeField] private float _radius;
    }
}
