using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _lifetime = 10f;
    public void Init(Vector3 direction, float speed)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = direction.normalized * speed;
        Debug.Log($"Bullet fired with velocity: {rb.linearVelocity}");
        Destroy(gameObject, _lifetime);
    }
}
