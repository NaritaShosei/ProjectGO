using UnityEngine;

public class BossPhaseController : MonoBehaviour
{
    public bool IsPhaseEnd => _phaseIndex >= _phases.Length;

    public void SetPhase()
    {
        _attackIndex = 0;
        _timer = 0f;

        if (!IsPhaseEnd)
        {
            _phaseIndex++;
        }
    }

    public void Tick()
    {
        if (_current == null || _current.Attacks.Length == 0) return;

        _timer += Time.deltaTime;

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
