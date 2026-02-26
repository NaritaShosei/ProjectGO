using UnityEngine;

public sealed class ElectrifiedCondition : IEnemyCondition
{
    public ConditionType Type => ConditionType.Electrified;
    
    // ボスじゃなかったら止まる
    public bool BlocksAction => !_enemyIsBoss;

    public bool IsFinished => _time <= 0f;

    public ElectrifiedCondition(
        float duration,
        bool enemyIsBoss)
    {
        _duration = duration;
        _enemyIsBoss = enemyIsBoss;
    }

    public void OnEnter(IEnemy enemy)
    {
        _time = _duration;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("感電開始");
#endif
    }

    public void Tick(IEnemy enemy, float dt)
    {
        _time -= dt;
    }

    public void OnExit(IEnemy enemy)
    {

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("感電終了");
#endif
    }

    // 持続時間
    private float _time;
    private readonly float _duration;
    private readonly bool _enemyIsBoss;
}
