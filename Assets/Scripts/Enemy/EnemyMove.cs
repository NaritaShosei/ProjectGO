using UnityEngine;
using UnityEngine.AI;

public class EnemyMove : MonoBehaviour
{
    [SerializeField] private float _distanceToPlayer = 2f;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private Transform _selfTransform;
    [SerializeField] private Transform _targetTransform;

    [Tooltip("目的地を再設定する最小距離（ターゲットがこの分だけ動いたら SetDestination）")]
    [SerializeField] private float _targetMoveThreshold = 0.5f;
    [Tooltip("目的地を再設定するインターバル")]
    [SerializeField] private float _updateInterval = 0.2f;

    private Vector3 _lastTargetPosition;
    private float _lastUpdateTime = 0f;
    private bool _isNearPlayer = false;

    public bool IsNearPlayerFlag => _isNearPlayer;

    public void Init(Transform playerTransform)
    {
        if (_navMeshAgent == null) _navMeshAgent = GetComponent<NavMeshAgent>();
        _targetTransform = playerTransform;
        _selfTransform = transform;

        if (_targetTransform != null)
        {
            _lastTargetPosition = _targetTransform.position;
            _lastUpdateTime = Time.time - _updateInterval;
        }
    }

    private void Update()
    {
        if (_targetTransform == null || _navMeshAgent == null) return;

        // 距離判定(SetDestinationの負荷を抑える目的)
        float distance = Vector3.Distance(_selfTransform.position, _targetTransform.position);
        _isNearPlayer = distance <= _distanceToPlayer;

        // 近ければ停止（Agent の挙動を止める）
        bool onSight = Physics.Raycast(_selfTransform.position, _targetTransform.position - _selfTransform.position, out RaycastHit hitInfo, _distanceToPlayer, LayerMask.GetMask("Default","Player"));
        if (_isNearPlayer && onSight)
        {
            _selfTransform.LookAt(new Vector3(_targetTransform.position.x, _selfTransform.position.y, _targetTransform.position.z));
            if (!_navMeshAgent.isStopped)
            {
                _navMeshAgent.isStopped = true;
            }
            return;
        }
        // 近くないなら追いかける。更新は閾値／間隔を満たした場合のみ
        if (Time.time - _lastUpdateTime >= _updateInterval)
        {
            float moved = Vector3.Distance(_lastTargetPosition, _targetTransform.position);
            if (moved >= _targetMoveThreshold || _navMeshAgent.pathPending || _navMeshAgent.remainingDistance <= 0f)
            {
                _navMeshAgent.isStopped = false;
                _navMeshAgent.SetDestination(_targetTransform.position);
                _lastTargetPosition = _targetTransform.position;
                _lastUpdateTime = Time.time;
            }
        }
    }
}
