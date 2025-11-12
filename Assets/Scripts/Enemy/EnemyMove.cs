using UnityEngine;
using UnityEngine.AI;
public class EnemyMove : MonoBehaviour
{
    public Transform _targetTransform;
    NavMeshAgent _navMeshAgent;

    void Start()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        _navMeshAgent.SetDestination(_targetTransform.position);
    }
}
