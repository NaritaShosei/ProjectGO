using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

using BossEnemy.Enum;
using BossEnemy.Effect;

namespace BossEnemy.Infrastructure
{
    public class AttackHitAreaSpawner : MonoBehaviour, IAttackHitAreaSpawner
    {
        public HitAreaView Spawn(AttackHitAreaType hitAreaType, Vector3 spawnCenterPos, float range, Vector3 forward = default)
        {
            if (hitAreaType == AttackHitAreaType.None)
            {
                Debug.LogError("HitAreaが設定されていません");
                return null;
            }

            HitAreaView hitArea = PrepareHitArea(hitAreaType, spawnCenterPos, forward);
            hitArea.ActiveView(range);

            return hitArea;
        }

        public HitAreaView SpawnSquare(Vector3 spawnCenterPos, float width, float length, Vector3 forward)
        {
            HitAreaView hitArea = PrepareHitArea(AttackHitAreaType.Square, spawnCenterPos, forward);
            if (hitArea is SquareHitAreaView squareHitArea)
                squareHitArea.SetSize(width, length);
            else
                Debug.LogError("SquareHitAreaView を取得できませんでした。");

            return hitArea;
        }

        [Header("円形のHitArea")]
        [SerializeField] private CircleHitAreaView _circleHitEffect;

        [Header("正方形のHitArea")]
        [SerializeField] private SquareHitAreaView _squareHitEffect;

        private Dictionary<AttackHitAreaType, Queue<HitAreaView>> _pool = new();

        private HitAreaView PrepareHitArea(
            AttackHitAreaType hitAreaType,
            Vector3 spawnCenterPos,
            Vector3 forward)
        {
            HitAreaView hitArea = GetHitArea(hitAreaType);
            Transform hitAreaTransform = hitArea.transform;

            // プールから返却された直後は非アクティブのままにし、ParticleSystem の
            // OnEnable より先に今回の位置と向きを設定する。
            hitAreaTransform.position = spawnCenterPos;
            if (forward.sqrMagnitude > 0.0001f)
                hitAreaTransform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

            hitArea.gameObject.SetActive(true);
            hitArea.OnDespawn += Release;
            return hitArea;
        }

        private HitAreaView GetHitArea(AttackHitAreaType hitAreaType)
        {
            HitAreaView hitArea = null;

            switch (hitAreaType)
            {
                case AttackHitAreaType.None:
                    Debug.LogError("該当するものがありません");
                    return null;
                case AttackHitAreaType.Circle:
                    if (TryGet(out hitArea, AttackHitAreaType.Circle))
                        return hitArea;

                    hitArea = Instantiate(_circleHitEffect);

                    if (hitArea != null)
                    {
                        hitArea.gameObject.transform.SetParent(gameObject.transform, true);
                        hitArea.gameObject.SetActive(false);
                    }
                    return hitArea;
                case AttackHitAreaType.Square:
                    if (TryGet(out hitArea, AttackHitAreaType.Square))
                        return hitArea;

                    hitArea = Instantiate(_squareHitEffect);

                    if (hitArea != null)
                    {
                        hitArea.gameObject.transform.SetParent(gameObject.transform, true);
                        hitArea.gameObject.SetActive(false);
                    }
                    return hitArea;
            }

            return null;
        }

        private bool TryGet(out HitAreaView result, AttackHitAreaType hitAreaType)
        {
            if (_pool.ContainsKey(hitAreaType))
            {
                if (_pool[hitAreaType].TryDequeue(out HitAreaView obj))
                {
                    result = obj;
                    return true;
                }
            }
            else
            {
                _pool.Add(hitAreaType, new Queue<HitAreaView>());
            }

            result = null;
            return false;
        }

        private void Release(HitAreaView hitArea, AttackHitAreaType hitAreaType)
        {
            hitArea.OnDespawn -= Release;
            _pool[hitAreaType]?.Enqueue(hitArea);
        }
    }
}
