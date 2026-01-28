using System.Collections.Generic;

public class EnemyBehaviourRunner
{
    private readonly List<IEnemyBehaviour> _behaviours = new();

    public void Add(IEnemyBehaviour behaviour)
    {
        _behaviours.Add(behaviour);
    }

    public void Tick(float deltaTime)
    {
        foreach (var behaviour in _behaviours)
        {
            behaviour.Tick(deltaTime);
        }
    }
}
