using UnityEngine;

namespace BossEnemy.Logic
{
    public class CirclePositionGenerator : MonoBehaviour
    {
        /// <summary>
        /// 3D空間（XZ平面：地面に平行な円）の円周上に均等配置された位置を取得します。
        /// </summary>
        /// <param name="center">中心点 (PositionA)</param>
        /// <param name="radius">半径</param>
        /// <param name="count">取得する点の個数</param>
        /// <returns>円周上の位置配列</returns>
        public static Vector3[] GetPositionsOnCircle3D(Vector3 center, float radius, int count)
        {
            if (count <= 0) return new Vector3[0];

            Vector3[] positions = new Vector3[count];

            // 1点あたりのステップ角度（ラジアン）
            float angleStep = (Mathf.PI * 2f) / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep;

                // XZ平面上に円を描く場合
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.z + Mathf.Sin(angle) * radius;

                positions[i] = new Vector3(x, center.y, z);
            }

            return positions;
        }
    }
}
