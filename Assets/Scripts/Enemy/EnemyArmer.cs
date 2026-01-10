using UnityEngine;

public class EnemyArmer : MonoBehaviour, IEnemy
{
    public bool IsBroken => _hp <= 0;
    public void AddKnockBackForce(Vector3 direction)
    {
        // ノックバックなし
    }

    public Transform GetTargetCenter()
    {
        return _targetCenter; ;
    }

    public void TakeDamage(AttackContext context)
    {
        if (context.PlayerMode != PlayerMode.Warrior) { return; }

        _hp -= context.Damage;

        if (_hp <= 0)
        {
            Break();
        }
    }

    [SerializeField] private float _hp = 50;
    [SerializeField] private GameObject _core;
    [SerializeField] private Transform _targetCenter;

    private void Break()
    {
        if (_core)
        {
            _core.SetActive(true);
        }

        gameObject.SetActive(false);
    }
}
