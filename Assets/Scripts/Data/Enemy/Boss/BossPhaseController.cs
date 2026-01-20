using UnityEngine;

public class BossPhaseController : MonoBehaviour
{
    public bool IsPhaseEnd => _phaseIndex >= _phases.Length;
    public BossPhaseData CurrentPhase => _current;

    public void Init(Transform player)
    {
        _player = player;
    }

    /// <summary>
    /// 次のフェーズへ遷移し、関連する状態を初期化する
    /// </summary>
    public void SetPhase()
    {
        // 状態のリセット
        _attackIndex = 0;
        _timer = 0f;

        // フェーズを進める
        _phaseIndex++;

        if (_phaseIndex < _phases.Length)
        {
            _current = _phases[_phaseIndex];
        }
    }

    /// <summary>
    /// Bossクラスのほうで呼ばれるUpdate関数の代わり
    /// </summary>
    public void Tick()
    {
        if (_current == null || _current.Attacks.Length == 0) return;

        _timer += Time.deltaTime;

        // データにある攻撃を順番に実行する
        var attack = _current.Attacks[_attackIndex];

        if (_timer >= attack.Interval)
        {
            _timer = 0f;

            attack.Execute(new BossAttackContext
            {
                BossTransform = transform,
                Player = _player
            });

            _attackIndex = (_attackIndex + 1) % _current.Attacks.Length;
        }
    }

    [SerializeField] private BossPhaseData[] _phases;
    private Transform _player;

    private BossPhaseData _current;
    private int _attackIndex;
    private int _phaseIndex;
    private float _timer;

    private void Start()
    {
        _current = _phases[0];
    }
}
