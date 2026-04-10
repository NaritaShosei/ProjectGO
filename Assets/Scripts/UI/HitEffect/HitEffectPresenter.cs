using System.Collections.Generic;
using UnityEngine;

public class HitEffectPresenter
{
    private Dictionary<HitEffectType, GameObject> _prefabDic;

    public HitEffectPresenter(Dictionary<HitEffectType, GameObject> prefabDic)
    {
        _prefabDic = prefabDic;
    }

    public void ShowHit(Vector3 position, HitEffectType type)
    {
        if (_prefabDic.TryGetValue(type, out var prefab))
        {
            var obj = Object.Instantiate(prefab);

            if (!obj.TryGetComponent<IHitEffectView>(out var view))
            {
                Debug.LogError($"IHitEffectView が見つかりません: {prefab.name}");
                Object.Destroy(obj);
                return;
            }
            view.EffectPlay(position);
        }
    }
}
