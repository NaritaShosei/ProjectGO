using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

using BossEnemy.Enum;
using BossEnemy.Effect;

namespace BossEnemy.Infrastructure
{
    public class AttackHitAreaSpawner : MonoBehaviour, IAttackHitAreaSpawner
    {
        public void Spawn(HitAreaType hitAreaType, Vector3 spawnCenterPos, float range, float despawnTime)
        {
            HitAreaViewBase hitArea = GetHitArea(hitAreaType);
            hitArea.gameObject.transform.position = spawnCenterPos;
            hitArea.OnDespawn += Release;
            hitArea.ActiveView(range, despawnTime);
        }

        [Header("円形のHitArea")]
        [SerializeField] private CircleHitAreaView _circleHitEffect;

        private Dictionary<HitAreaType, Queue<HitAreaViewBase>> _pool = new();

        private HitAreaViewBase GetHitArea(HitAreaType hitAreaType)
        {
            HitAreaViewBase hitArea = null;

            switch (hitAreaType)
            {
                case HitAreaType.None:
                    Debug.LogError("該当するものがありません");
                    return null;
                case HitAreaType.Circle:
                    if (TryGet(out hitArea, HitAreaType.Circle))
                    {
                        hitArea.gameObject.SetActive(true);
                        return hitArea;
                    }

                    hitArea = Instantiate(_circleHitEffect);

                    if (hitArea != null)
                        hitArea.gameObject.transform.SetParent(gameObject.transform, true);
                    return hitArea;
            }

            return null;
        }

        private bool TryGet(out HitAreaViewBase result, HitAreaType hitAreaType)
        {
            if (_pool.ContainsKey(hitAreaType))
            {
                if (_pool[hitAreaType].TryDequeue(out HitAreaViewBase obj))
                {
                    result = obj;
                    return true;
                }
            }
            else
            {
                _pool.Add(hitAreaType, new Queue<HitAreaViewBase>());
            }

            result = null;
            return false;
        }

        private void Release(HitAreaViewBase hitArea, HitAreaType hitAreaType)
        {
            hitArea.OnDespawn -= Release;
            _pool[hitAreaType].Enqueue(hitArea);
        }
    }

}
