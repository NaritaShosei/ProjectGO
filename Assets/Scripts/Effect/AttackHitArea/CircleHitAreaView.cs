using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

using BossEnemy.Enum;

namespace BossEnemy.Effect
{
    public class CircleHitAreaView : HitAreaView
    {
        public override event Action<HitAreaView, AttackHitAreaType> OnDespawn;

        public override void ActiveView(float radius)
        {
            SetRange(radius);
        }

        public override void SetRange(float radius)
        {
            // AttackHitAreaRadius は実際の円形当たり判定の半径。
            // 表示側も同じ半径を使う。
            GenerateRadialMesh(_simpleRedLoop, radius, false);
            GenerateRadialMesh(_lateralGradient, radius, true);
            GenerateRadialMesh(_sheen, radius, true);

            // Build では Disabled の ProceduralMeshGenerator が OnEnable されず、
            // ParticleSystemRenderer.m_Mesh が未設定になる。明示生成した実寸 Mesh を
            // 使うため、ルートの拡縮は行わない。
            transform.localScale = _initialLocalScale;
        }

        public override void InVisible()
        {
            OnDespawn?.Invoke(this, AttackHitAreaType.Circle);
            this.gameObject.SetActive(false);
        }

        private Vector3 _initialLocalScale;

        [SerializeField] private ProceduralMeshGenerator _simpleRedLoop;
        [SerializeField] private ProceduralMeshGenerator _lateralGradient;
        [SerializeField] private ProceduralMeshGenerator _sheen;


        private void Awake()
        {
            _initialLocalScale = transform.localScale;
        }

        private static void GenerateRadialMesh(
            ProceduralMeshGenerator generator,
            float radius,
            bool compensateParticleSize)
        {
            if (generator == null) return;

            // LateralGradient / Sheen は ParticleSystem の startSize が 0.49 のため、
            // Mesh 半径をそのまま設定すると Outline より小さく描画される。
            float meshRadius = radius;
            if (compensateParticleSize && generator.TryGetComponent(out ParticleSystem particleSystem))
            {
                float startSize = particleSystem.main.startSize.constant;
                if (startSize > 0.0001f)
                    meshRadius /= startSize;
            }

            generator.outerRadius = meshRadius;
            generator.GenerateMesh();
        }
    }
}
