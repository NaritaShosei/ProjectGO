using System;
using UnityEngine;

public class EnemyBullet : MonoBehaviour, IPoolable, ISpeedChange
{
    [SerializeField] private float _lifetime = 10f;
    private AttackData _attackData;
    private float _timer;
    public Action OnRelease { get; set; }
    public float TimeScale { get; set; } = 1.0f;

    public void Init(Vector3 direction, AttackData attackData)
    {
        _attackData = attackData;
        gameObject.SetActive(true);
        Rigidbody rb = GetComponent<Rigidbody>();
        direction.y = 0f; // 高さ方向の影響を無視
        rb.linearVelocity = direction.normalized * _attackData.Speed;
        _timer = 0f;
    }
    private void Update()
    {
        _timer += Time.deltaTime * TimeScale;
        if (_timer >= _lifetime) ReturnToPool();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IPlayer>(out var player))
        {
            player.TakeDamage(_attackData.Power);
            Debug.Log($"Bullet hit {player} for {_attackData.Power} damage.");
            ReturnToPool();
        }

    }
    void ReturnToPool()
    {
        OnRelease?.Invoke();
    }
    public void OnSpeedChange(float scale)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = rb.linearVelocity.normalized * _attackData.Speed * scale;
        TimeScale = scale;
        Debug.Log($"BulletSpeed = {rb.linearVelocity}");
    }
}