using UnityEngine;

[CreateAssetMenu(fileName = "BossPhaseData", menuName = "GameData/BossPhaseData")]

public class BossPhaseData : ScriptableObject
{
    public BossAttackBase[] Attacks => _attacks;
    public EnemyData Data => _data;

    [SerializeField] private BossAttackBase[] _attacks;
    [SerializeField] private EnemyData _data;
}
