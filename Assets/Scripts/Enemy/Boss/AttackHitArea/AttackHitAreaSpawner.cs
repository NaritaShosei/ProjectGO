using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitAreaSpawner : MonoBehaviour, IAttackHitAreaSpawner
{
    public void Spawn(HitAreaType hitAreaType, Vector3 spawnCenterPos, float range, float despawnTime)
    {
        HitAreaBase hitArea = GetHitArea(hitAreaType);
        hitArea.gameObject.transform.position = spawnCenterPos;
        hitArea.OnDespawn += Release;
        hitArea.SetRange(range);
        hitArea.SetDespawnTime(despawnTime);
        hitArea.ActiveView().Forget();
    }

    [Header("円形のHitArea")]
    [SerializeField] private HitAreaBase _circleHitEffect;

    private Dictionary<HitAreaType, Queue<HitAreaBase>> _pool = new();

    private HitAreaBase GetHitArea(HitAreaType hitAreaType)
    {
        HitAreaBase hitArea = null;

        switch (hitAreaType)
        {
            case HitAreaType.None:
                Debug.LogError("該当するものがありません");
                return null;
            case HitAreaType.Circle:
                if(TryGet(out hitArea, HitAreaType.Circle))
                {
                    hitArea.gameObject.SetActive(true);
                    return hitArea;
                }
                return hitArea = Instantiate(_circleHitEffect);
        }

        return null;
    }

    private bool TryGet(out HitAreaBase result, HitAreaType hitAreaType)
    {
        if (_pool.ContainsKey(hitAreaType))
        {
            if (_pool[hitAreaType].TryDequeue(out HitAreaBase obj))
            {
                result = obj;
                return true;
            }
        }
        else
        {
            _pool.Add(hitAreaType, new Queue<HitAreaBase>());
        }

        result = null;
        return false;
    }

    private void Release(HitAreaBase hitArea, HitAreaType hitAreaType)
    {
        hitArea.OnDespawn -= Release;
        _pool[hitAreaType].Enqueue(hitArea);
    }
}
