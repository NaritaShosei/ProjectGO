using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockDamageSkill", menuName = "GameData/Skill/BlockDamageSkill")]

public class BlockDamageSkill : SkillBase,IDamageModifier,IDamageReactionModifier
{
    public override void OnAcquire(IPlayerStats stats)
    {
        stats.AddDamageModifier(this);
        stats.AddDamageReactionModifier(this);
    }

    public void Modify(ref float damage, PlayerMode mode)
    {
        if (!_isCoolDown)
        {
            damage = 0;
            _isCoolDown = true;

            Debug.Log($"被ダメージを{damage}に軽減");

            StartCoolDown(_recastTime).Forget();
        }
    }

    public bool CanInterrupt(PlayerState state)
    {
        return false;
    }

    [SerializeField] private float _recastTime = 10;
    private bool _isCoolDown = false;

    private async UniTaskVoid StartCoolDown(float recastTime)
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(recastTime));
        _isCoolDown = false;
    }
}
