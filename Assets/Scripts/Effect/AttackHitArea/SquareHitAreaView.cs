using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

using BossEnemy.Enum;

namespace BossEnemy.Effect
{
    public class SquareHitAreaView : HitAreaView
    {
        public override event Action<HitAreaView, AttackHitAreaType> OnDespawn;

        public override void ActiveView(float range, float despawnTime)
        {
            SetRange(range);
            SetDespawnTime(despawnTime);
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
            SetQuadSize(_squareFangsLoop, width, length);
            SetQuadSize(_squareFangsGlow, width, length);
            SetQuadSize(_squareFangsSheen1, width, length);
            SetQuadSize(_squareFangsSheen2, width, length);
            SetQuadSize(_simpleSquareLateralGradient, width, length);

            // ProceduralMeshGenerator は実行中に quadSize を変更しても、メッシュを自動で再生成しない。
            // 描画済みメッシュを確実に範囲へ追従させるため、初期メッシュ寸法に対する比率でルートを拡縮する。
            float widthScale = _initialMeshSize.x > 0f ? width / _initialMeshSize.x : 1f;
            float lengthScale = _initialMeshSize.y > 0f ? length / _initialMeshSize.y : 1f;
            transform.localScale = new Vector3(
                _initialLocalScale.x * widthScale,
                _initialLocalScale.y,
                _initialLocalScale.z * lengthScale);
        }

        public override void SetDespawnTime(float despawnTime)
        {
            _despawnTime = despawnTime;
        }

        public override void Despawn()
        {
            OnDespawn?.Invoke(this, AttackHitAreaType.Square);
            this.gameObject.SetActive(false);
        }

        private float _despawnTime;
        private float _elapsedTime;
        private Vector2 _initialMeshSize;
        private Vector3 _initialLocalScale;

        private void Awake()
        {
            _initialMeshSize = _squareFangsLoop.quadSize;
            _initialLocalScale = transform.localScale;
        }

        private static void SetQuadSize(ProceduralMeshGenerator mesh, float width, float length)
        {
            Vector2 size = mesh.quadSize;
            size.x = width;
            size.y = length;
            mesh.quadSize = size;
        }

        [SerializeField] private ProceduralMeshGenerator _squareFangsLoop;
        [SerializeField] private ProceduralMeshGenerator _squareFangsGlow;
        [SerializeField] private ProceduralMeshGenerator _squareFangsSheen1;
        [SerializeField] private ProceduralMeshGenerator _squareFangsSheen2;
        [SerializeField] private ProceduralMeshGenerator _simpleSquareLateralGradient;


        private void OnEnable()
        {
            _elapsedTime = 0;
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
