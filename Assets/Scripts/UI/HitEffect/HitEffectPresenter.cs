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

            var view = obj.GetComponent<IHitEffectView>();
            view.EffectPlay(position);
        }
    }
}
