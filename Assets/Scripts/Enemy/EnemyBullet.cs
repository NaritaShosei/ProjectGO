using System;
using UnityEngine;

public class EnemyBullet : MonoBehaviour, IPoolable
{
    [SerializeField] private float _lifetime = 10f;
    private AttackData _attackData;
    private float _timer;
    public Action OnRelease { get; set; }

    public void Init(Vector3 direction,AttackData attackData)
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
        _timer += Time.deltaTime;
        if (_timer >= _lifetime) ReturnToPool();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IPlayer>(out var player))
        {
            player.AddDamage(_attackData.Power);
            Debug.Log($"Bullet hit {player} for {_attackData.Power} damage.");
            ReturnToPool();
        }

    }
    void ReturnToPool()
    {
        OnRelease?.Invoke();
    }
}