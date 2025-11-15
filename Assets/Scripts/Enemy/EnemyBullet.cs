using System;
using UnityEngine;

public class EnemyBullet : MonoBehaviour, IPoolable
{
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _lifetime = 10f;
    private float _timer;
    public Action OnRelease { get; set; }
    public ObjectPoolManager PoolManager { get; set; }

    public void Init(Vector3 direction, float speed)
    {
        gameObject.SetActive(true);
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = direction.normalized * speed;
        _timer = 0f;
        Debug.Log($"Bullet fired with velocity: {rb.linearVelocity}");
    }
    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _lifetime) ReturnToPool();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerManager>(out var player))
        {
            //player.TakeDamage(_damage);
            ReturnToPool();
        }

    }
    void ReturnToPool()
    {
        OnRelease?.Invoke();
    }
}