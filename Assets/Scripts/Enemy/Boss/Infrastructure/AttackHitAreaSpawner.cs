using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

using BossEnemy.Enum;
using BossEnemy.Effect;

namespace BossEnemy.Infrastructure
{
    public class AttackHitAreaSpawner : MonoBehaviour, IAttackHitAreaSpawner
    {
        public void Spawn(AttackHitAreaType hitAreaType, Vector3 spawnCenterPos, float range, float despawnTime)
        {
            HitAreaViewBase hitArea = GetHitArea(hitAreaType);
            hitArea.gameObject.transform.position = spawnCenterPos;
            hitArea.OnDespawn += Release;
            hitArea.ActiveView(range, despawnTime);
        }

        [Header("円形のHitArea")]
        [SerializeField] private CircleHitAreaView _circleHitEffect;

        private Dictionary<AttackHitAreaType, Queue<HitAreaViewBase>> _pool = new();

        private HitAreaViewBase GetHitArea(AttackHitAreaType hitAreaType)
        {
            HitAreaViewBase hitArea = null;

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
            }

            return null;
        }

        private bool TryGet(out HitAreaViewBase result, AttackHitAreaType hitAreaType)
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

        private void Release(HitAreaViewBase hitArea, AttackHitAreaType hitAreaType)
        {
            hitArea.OnDespawn -= Release;
            _pool[hitAreaType].Enqueue(hitArea);
        }
    }

}
