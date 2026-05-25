using UnityEngine;

/// <summary>
/// 感電状態のCondition
/// 持続時間中、PerlinNoiseによる振動でEnemyの見た目を揺らす
/// ボスでなければBlocksActionがtrueになり行動を停止する
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
        if (duration <= 0f)
            throw new System.ArgumentOutOfRangeException(nameof(duration), "duration must be positive");
        _duration = duration;
        _enemyIsBoss = enemyIsBoss;
    }

    public void OnEnter(IEnemy enemy)
    {
        _time = _duration;
        _baseLocalPos = enemy.Self.localPosition;
        enemy.EnemyAnimator?.SetElectrified(true);

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

        enemy.Self.localPosition = _baseLocalPos + offset;
    }

    public void OnExit(IEnemy enemy)
    {
        enemy.Self.localPosition = _baseLocalPos;
        enemy.EnemyAnimator?.SetElectrified(false);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("感電終了");
#endif
    }

    private readonly float _duration;
    private readonly bool _enemyIsBoss;

    // 調整値
    private const float _maxShake = 0.08f;
    private const float _frequency = 40f;

    // 残り時間・揺れ計算用（OnEnterで初期化）
    private float _time;
    private Vector3 _baseLocalPos;
    private float _noiseTime;
}
