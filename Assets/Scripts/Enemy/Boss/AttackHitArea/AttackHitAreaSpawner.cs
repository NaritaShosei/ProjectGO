using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitAreaSpawner : MonoBehaviour, IAttackHitAreaSpawner
{
    public void Spawn(HitAreaType hitAreaType, Vector3 spawnCenterPos, float range, float despawnTime)
    {
        CircleHitAreaView hitArea = GetHitArea(hitAreaType);
        hitArea.gameObject.transform.position = spawnCenterPos;
        hitArea.OnDespawn += Release;
        hitArea.SetRange(range);
        hitArea.SetDespawnTime(despawnTime);
        hitArea.ActiveView().Forget();
    }

    [Header("円形のHitArea")]
    [SerializeField] private CircleHitAreaView _circleHitEffect;

    private Dictionary<HitAreaType, Queue<CircleHitAreaView>> _pool = new();

    private CircleHitAreaView GetHitArea(HitAreaType hitAreaType)
    {
        CircleHitAreaView hitArea = null;

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

    private bool TryGet(out CircleHitAreaView result, HitAreaType hitAreaType)
    {
        if (_pool.ContainsKey(hitAreaType))
        {
            if (_pool[hitAreaType].TryDequeue(out CircleHitAreaView obj))
            {
                result = obj;
                return true;
            }
        }

        result = null;
        return false;
    }

    private void Release(HitAreaBase hitArea)
    {
        hitArea.OnDespawn -= Release;

    }
}
