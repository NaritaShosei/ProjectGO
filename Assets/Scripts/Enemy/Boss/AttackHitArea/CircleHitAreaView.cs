using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class CircleHitAreaView : HitAreaBase
{
    public override event Action<HitAreaBase, HitAreaType> OnDespawn;

    public async override UniTask ActiveView()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(_despawnTime));

        Despawn();
    }

    public override void SetRange(float range)
    {
        _simpleRedLoop.innerRadius = _minInnerRadius;
        _lateralGradient.innerRadius = _minInnerRadius;
        _sheen.innerRadius = _minInnerRadius;

        _simpleRedLoop.outerRadius = range / 2;
        _lateralGradient.outerRadius = range;
        _sheen.outerRadius = range;
    }

    public override void SetDespawnTime(float despawnTime)
    {
        _despawnTime = despawnTime;
    }

    public override void Despawn()
    {
        this.gameObject.SetActive(false);
        OnDespawn?.Invoke(this, HitAreaType.Circle);
    }

    private float _despawnTime;

    private const float _minInnerRadius = 0.001f;

    [SerializeField] private ProceduralMeshGenerator _simpleRedLoop;
    [SerializeField] private ProceduralMeshGenerator _lateralGradient;
    [SerializeField] private ProceduralMeshGenerator _sheen;
}
