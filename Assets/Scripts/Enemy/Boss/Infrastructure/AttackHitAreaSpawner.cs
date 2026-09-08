using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

using BossEnemy.Enum;
using BossEnemy.Effect;

namespace BossEnemy.Infrastructure
{
    public class AttackHitAreaSpawner : MonoBehaviour, IAttackHitAreaSpawner
    {
        public HitAreaView Spawn(AttackHitAreaType hitAreaType, Vector3 spawnCenterPos, float range, float despawnTime, Vector3 forward = default)
        {
            HitAreaView hitArea = GetHitArea(hitAreaType);
            hitArea.gameObject.transform.position = spawnCenterPos;
            hitArea.gameObject.transform.forward = forward;
            hitArea.OnDespawn += Release;
            hitArea.ActiveView(range, despawnTime);

            return hitArea;
        }

        [Header("円形のHitArea")]
        [SerializeField] private CircleHitAreaView _circleHitEffect;

        [Header("正方形のHitArea")]
        [SerializeField] private SquareHitAreaView _squareHitEffect;

        private Dictionary<AttackHitAreaType, Queue<HitAreaView>> _pool = new();

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
                    {
                        hitArea.gameObject.SetActive(true);
                        return hitArea;
                    }

                    hitArea = Instantiate(_circleHitEffect);

                    if (hitArea != null)
                        hitArea.gameObject.transform.SetParent(gameObject.transform, true);
                    return hitArea;
                case AttackHitAreaType.Square:
                    if (TryGet(out hitArea, AttackHitAreaType.Square))
                    {
                        hitArea.gameObject.SetActive(true);
                        return hitArea;
                    }

                    hitArea = Instantiate(_squareHitEffect);

                    if (hitArea != null)
                        hitArea.gameObject.transform.SetParent(gameObject.transform, true);
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
