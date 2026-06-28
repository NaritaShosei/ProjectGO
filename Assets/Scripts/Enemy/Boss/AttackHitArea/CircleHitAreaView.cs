using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class CircleHitAreaView : HitAreaBase
{
    public override event Action<HitAreaBase, HitAreaType> OnDespawn;

    public override void ActiveView(float range, float despawnTime)
    {
        SetRange(range);
        SetDespawnTime(despawnTime);
    }

    public override void SetRange(float range)
    {
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
        OnDespawn?.Invoke(this, HitAreaType.Circle);
        this.gameObject.SetActive(false);
    }

    private float _despawnTime;
    private float _elapsedTime;

    private const float _minInnerRadius = 0.001f;
    private const float _minOuterRadius = 0.002f;

    [SerializeField] private ProceduralMeshGenerator _simpleRedLoop;
    [SerializeField] private ProceduralMeshGenerator _lateralGradient;
    [SerializeField] private ProceduralMeshGenerator _sheen;

    private void OnEnable()
    {
        _elapsedTime = 0;
    }

    private void Awake()
    {
        _simpleRedLoop.innerRadius = _minInnerRadius;
        _lateralGradient.innerRadius = _minInnerRadius;
        _sheen.innerRadius = _minInnerRadius;

        _simpleRedLoop.outerRadius = _minOuterRadius;
        _lateralGradient.outerRadius = _minOuterRadius;
        _sheen.outerRadius = _minOuterRadius;
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
