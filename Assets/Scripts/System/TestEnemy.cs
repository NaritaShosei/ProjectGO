// TestEnemy.cs（テスト用）
using UnityEngine;

public class TestEnemy : MonoBehaviour, ILockOnTarget
{
    [SerializeField] private Transform _lockOnPoint;

    public Transform LockOnPoint => _lockOnPoint != null ? _lockOnPoint : transform;
    public bool IsLockable => true; // テスト用なので常にtrue

    public Transform GetTargetCenter()
    {
        return LockOnPoint;
    }
}
