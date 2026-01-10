using UnityEngine;

[CreateAssetMenu(fileName = "BossPhaseData", menuName = "GameData/BossPhaseData")]

public class BossPhaseData : ScriptableObject
{
    public BossAttackBase[] Attacks => _attacks;
    [SerializeField] private BossAttackBase[] _attacks;
}
