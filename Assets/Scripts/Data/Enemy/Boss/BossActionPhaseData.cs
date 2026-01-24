using UnityEngine;

[CreateAssetMenu(fileName = "BossActionPhaseData", menuName = "GameData/BossActionPhase")]

public class BossActionPhaseData : ScriptableObject
{
    public BossAttackBase[] Attacks => _attacks;
    public EnemyData Data => _data;

    [SerializeField] private BossAttackBase[] _attacks;
    [SerializeField] private EnemyData _data;
}
