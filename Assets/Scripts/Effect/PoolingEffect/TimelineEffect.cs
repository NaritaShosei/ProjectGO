using UnityEngine;
using UnityEngine.Playables;

public class TimelineEffect : EffectBase
{
    [SerializeField] private PlayableDirector _director;

    protected override void Awake()
    {
        base.Awake();

        if (_director == null)
            _director = GetComponent<PlayableDirector>();
    }

    protected override void OnPlayInternal()
    {
        if (_director == null) return;

        _director.time = 0;
        _director.Play();
    }

    protected override void ApplyScaleInternal(Vector3 scale)
    {
        transform.localScale = scale;
    }

    protected override void OnStopInternal()
    {
        if (_director == null) return;

        _director.Stop();
        _director.time = 0;
        _director.Evaluate();
    }

    protected override bool IsAliveInternal()
    {
        if (_director == null) return false;

        return _director.state == PlayState.Playing;
    }

    protected override void ApplyTimeScaleInternal(float scale)
    {
        if (_director == null) return;

        var graph = _director.playableGraph;

        if (!graph.IsValid())
            return;

        if (graph.GetRootPlayableCount() == 0)
            return;

        var root = graph.GetRootPlayable(0);

        if (root.IsValid())
        {
            root.SetSpeed(scale);
        }
    }
}
