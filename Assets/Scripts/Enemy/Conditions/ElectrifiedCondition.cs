using UnityEngine;

/// <summary>
/// 色を変える方針からぶるぶるさせるように変更した
/// 最後もこのまま使用でききるかも？
/// 色をやめたのはIEnemyにもう何も追加したくないから。。
/// </summary>
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
        _baseLocalPos = enemy.GetTargetCenter().localPosition;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("感電開始");
#endif
    }

    public void Tick(IEnemy enemy, float dt)
    {
        _time -= dt;

        // 0→1 の残存割合
        float normalized = Mathf.Clamp01(_time / _duration);

        // 徐々に弱まる
        float intensity = _maxShake * normalized;

        // ノイズ振動（Randomより自然）
        _noiseTime += dt * _frequency;

        float x = (Mathf.PerlinNoise(_noiseTime, 0f) - 0.5f) * 2f;
        float z = (Mathf.PerlinNoise(0f, _noiseTime) - 0.5f) * 2f;

        Vector3 offset = new Vector3(x, 0f, z) * intensity;

        enemy.GetTargetCenter().localPosition = _baseLocalPos + offset;
    }

    public void OnExit(IEnemy enemy)
    {
        enemy.GetTargetCenter().localPosition = _baseLocalPos;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("感電終了");
#endif
    }

    // 持続時間
    private float _time;
    private readonly float _duration;
    private readonly bool _enemyIsBoss;


    private Vector3 _baseLocalPos;

    private float _noiseTime;

    // 調整値
    private const float _maxShake = 0.08f;  // 揺れ幅
    private const float _frequency = 40f;   // 速さ
}
