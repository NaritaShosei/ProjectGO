using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

using BossEnemy.Enum;

namespace BossEnemy.Effect
{
    public class CircleHitAreaView : HitAreaView
    {
        public override event Action<HitAreaView, AttackHitAreaType> OnDespawn;

        public override void ActiveView(float range, float despawnTime )
        {
            SetRange(range);
            SetDespawnTime(despawnTime);
        }

        public override void SetRange(float range)
        {
            // AttackHitAreaRadius は実際の円形当たり判定の半径。
            // 表示側も同じ半径を使う。
            _simpleRedLoop.outerRadius = range;
            _lateralGradient.outerRadius = range;
            _sheen.outerRadius = range;

            // ProceduralMeshGenerator は outerRadius の変更だけでは、生成済みの
            // メッシュを再生成しない。そのため、プレハブの描画済み半径を基準に
            // ルートを拡縮して、見た目を実判定半径へ一致させる。
            float scale = _initialMeshRadius > 0f ? range / _initialMeshRadius : 1f;
            transform.localScale = _initialLocalScale * scale;
        }

        public override void SetDespawnTime(float despawnTime)
        {
            _despawnTime = despawnTime;
        }

        public override void Despawn()
        {
            OnDespawn?.Invoke(this, AttackHitAreaType.Circle);
            this.gameObject.SetActive(false);
        }

        private float _despawnTime;
        private float _elapsedTime;
        private float _initialMeshRadius;
        private Vector3 _initialLocalScale;

        private const float _minInnerRadius = 0.001f;
        private const float _minOuterRadius = 0.002f;

        [SerializeField] private ProceduralMeshGenerator _simpleRedLoop;
        [SerializeField] private ProceduralMeshGenerator _lateralGradient;
        [SerializeField] private ProceduralMeshGenerator _sheen;

        private void OnEnable()
        {
            _elapsedTime = 0;
        }

        private void Awake()
        {
            _initialLocalScale = transform.localScale;
            _initialMeshRadius = GetInitialMeshRadius();

            _simpleRedLoop.innerRadius = _minInnerRadius;
            _lateralGradient.innerRadius = _minInnerRadius;
            _sheen.innerRadius = _minInnerRadius;

            _simpleRedLoop.outerRadius = _minOuterRadius;
            _lateralGradient.outerRadius = _minOuterRadius;
            _sheen.outerRadius = _minOuterRadius;
        }

        /// <summary>
        /// 外部の ProceduralMeshGenerator は実行時の半径変更でメッシュを作り直さない。
        /// そのため設定値ではなく、実際に描画されている MeshFilter の頂点範囲から
        /// プレハブ固有の円半径を取得する。
        /// </summary>
        private float GetInitialMeshRadius()
        {
            float radius = 0f;

            foreach (MeshFilter meshFilter in GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null) continue;

                Bounds bounds = mesh.bounds;
                Vector3 center = bounds.center;
                Vector3 extents = bounds.extents;

                // 円形メッシュの四方の端をルートローカル座標へ変換し、XZ 平面の
                // 実半径を測る。子 Transform の拡縮・回転にも対応する。
                radius = Mathf.Max(radius, GetLocalXZDistance(meshFilter, center + Vector3.right * extents.x));
                radius = Mathf.Max(radius, GetLocalXZDistance(meshFilter, center - Vector3.right * extents.x));
                radius = Mathf.Max(radius, GetLocalXZDistance(meshFilter, center + Vector3.forward * extents.z));
                radius = Mathf.Max(radius, GetLocalXZDistance(meshFilter, center - Vector3.forward * extents.z));
            }

            if (radius > 0f) return radius;

            // メッシュが未生成の場合だけ、プレハブ設定値を安全な代替値にする。
            return Mathf.Max(
                _simpleRedLoop.outerRadius,
                _lateralGradient.outerRadius,
                _sheen.outerRadius);
        }

        private float GetLocalXZDistance(MeshFilter meshFilter, Vector3 meshLocalPoint)
        {
            Vector3 point = transform.InverseTransformPoint(
                meshFilter.transform.TransformPoint(meshLocalPoint));
            return new Vector2(point.x, point.z).magnitude;
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= _despawnTime)
            {
                Despawn();
            }
        }
    }
}
