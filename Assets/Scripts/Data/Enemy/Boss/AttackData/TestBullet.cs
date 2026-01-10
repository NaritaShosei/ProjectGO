using UnityEngine;

public class TestBullet : MonoBehaviour
{
    public void Init(float damage)
    {
        _damage = damage;
        Destroy(gameObject, 10);
    }

    private float _damage;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out IPlayer player))
        {
            player.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }
}
