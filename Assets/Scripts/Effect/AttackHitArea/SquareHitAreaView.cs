using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

using BossEnemy.Enum;

namespace BossEnemy.Effect
{
    public class SquareHitAreaView : HitAreaView
    {
        public override event Action<HitAreaView, AttackHitAreaType> OnDespawn;

        public override void ActiveView(float range)
        {
            SetRange(range);
        }

        public override void SetRange(float range)
        {
            SetSize(_squareFangsLoop.quadSize.x, range);
        }

        /// <summary>
        /// 正方形表示の幅と長さを設定する。突進攻撃のように、幅と到達距離が異なる範囲表示に使用する。
        /// </summary>
        public void SetSize(float width, float length)
        {
            // 描画用ネストPrefabはローカルYで90°回転している。
            // その座標系ではquadSize.xが攻撃方向、quadSize.yが横幅になる。
            SetQuadSize(_squareFangsLoop, length, width);
            SetQuadSize(_squareFangsGlow, length, width);
            SetQuadSize(_squareFangsSheen1, length, width);
            SetQuadSize(_squareFangsSheen2, length, width);
            SetQuadSize(_simpleSquareLateralGradient, length, width);

            // quadSize の変更を描画用 Mesh へ反映する。
            GenerateMesh(_squareFangsLoop);
            GenerateMesh(_squareFangsGlow);
            GenerateMesh(_squareFangsSheen1);
            GenerateMesh(_squareFangsSheen2);
            GenerateMesh(_simpleSquareLateralGradient);
            transform.localScale = _initialLocalScale;
            UpdateOutline(width, length);
            RestartParticleSystems();
        }

        public override void InVisible()
        {
            OnDespawn?.Invoke(this, AttackHitAreaType.Square);
            this.gameObject.SetActive(false);
        }

        [SerializeField] private ProceduralMeshGenerator _squareFangsLoop;
        [SerializeField] private ProceduralMeshGenerator _squareFangsGlow;
        [SerializeField] private ProceduralMeshGenerator _squareFangsSheen1;
        [SerializeField] private ProceduralMeshGenerator _squareFangsSheen2;
        [SerializeField] private ProceduralMeshGenerator _simpleSquareLateralGradient;
        [SerializeField, Min(0.001f)] private float _outlineWidth = 0.04f;
        [SerializeField] private Color _outlineColor = new Color(1f, 0.05f, 0.05f, 1f);
        [SerializeField, Range(0f, 1f)] private float _outlineOpacity = 1f;

        private Vector3 _initialLocalScale;
        private LineRenderer _outlineRenderer;

        private void Awake()
        {
            _initialLocalScale = transform.localScale;
            CreateOutlineRenderer();
        }

        private static void SetQuadSize(ProceduralMeshGenerator mesh, float width, float length)
        {
            if (mesh == null) return;

            Vector2 size = mesh.quadSize;
            size.x = width;
            size.y = length;
            mesh.quadSize = size;
        }

        private static void GenerateMesh(ProceduralMeshGenerator generator)
        {
            if (generator == null) return;
            generator.GenerateMesh();
        }

        /// <summary>
        /// Square のみ、範囲の外周を常に視認できるように矩形アウトラインを重ねる。
        /// Square Fangs の既存マテリアルを使うため、Build に新しい Shader 依存を増やさない。
        /// </summary>
        private void CreateOutlineRenderer()
        {
            GameObject outlineObject = new GameObject("SquareHitAreaOutline");
            outlineObject.transform.SetParent(transform, false);

            _outlineRenderer = outlineObject.AddComponent<LineRenderer>();
            _outlineRenderer.useWorldSpace = false;
            _outlineRenderer.loop = true;
            _outlineRenderer.positionCount = 4;
            _outlineRenderer.widthMultiplier = _outlineWidth;
            _outlineRenderer.numCornerVertices = 2;
            _outlineRenderer.numCapVertices = 0;
            ApplyOutlineColor();

            if (_squareFangsLoop != null &&
                _squareFangsLoop.TryGetComponent(out ParticleSystemRenderer particleRenderer))
            {
                _outlineRenderer.sharedMaterial = particleRenderer.sharedMaterial;
            }
        }

        private void UpdateOutline(float width, float length)
        {
            if (_outlineRenderer == null) return;

            float halfWidth = width * 0.5f;
            float halfLength = length * 0.5f;
            const float outlineHeight = 0.01f;

            _outlineRenderer.SetPositions(new[]
            {
                new Vector3(-halfWidth, outlineHeight, -halfLength),
                new Vector3(-halfWidth, outlineHeight, halfLength),
                new Vector3(halfWidth, outlineHeight, halfLength),
                new Vector3(halfWidth, outlineHeight, -halfLength),
            });
            ApplyOutlineColor();
        }

        private void ApplyOutlineColor()
        {
            if (_outlineRenderer == null) return;

            Color color = _outlineColor;
            color.a *= _outlineOpacity;
            _outlineRenderer.startColor = color;
            _outlineRenderer.endColor = color;
        }

        /// <summary>
        /// プールから再利用した際、古い座標・向きの World Space 粒子を残さず、
        /// Spawn 後に設定された Transform で生成し直す。
        /// </summary>
        private void RestartParticleSystems()
        {
            foreach (ParticleSystem particleSystem in GetComponentsInChildren<ParticleSystem>(true))
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Play(true);
            }
        }
    }
}
