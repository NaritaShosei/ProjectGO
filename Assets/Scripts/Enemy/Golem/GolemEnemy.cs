using UnityEngine;

public class GolemEnemy : MobEnemy
{
    [SerializeField]
    private float _downDuration = 5f;

    [SerializeField]
    private MobArmor _armor;

    private bool _isDown;

    public override void Init()
    {
        base.Init();

        OnArmorBroken += HandleArmorBroken;
    }

    protected override void OnDestroy()
    {
        OnArmorBroken -= HandleArmorBroken;

        base.OnDestroy();
    }

    private void HandleArmorBroken(IEnemy enemy)
    {
        _isDown = true;

        ConditionController.ApplyCondition(
            new DownCondition(_downDuration));
    }

    public void RecoverArmor()
    {
        _isDown = false;

        if (_armor == null)
        {
            Debug.LogWarning("Armor is null.");
            return;
        }

        _armor.Restore();
        _defenceContext.EnemyType = EnemyDefenceType.Armor;
    }

    public override void TakeDamage(DamageContext context)
    {
        if(_isDown)
        {
            return;
        }

        int damage =
    DamageSystem.CalculateDamage(context,_defenceContext);

        if (!_isDown)
        {
            _armor.AbsorbDamageAndReturnExcess(damage);
            return;
        }

        _stats.TakeDamage(damage);

        if (_stats.CurrentHealth <= 0)
        {
            _stats.Kill();
        }
    }

    public override void OnConditionInterrupt()
    {
        base.OnConditionInterrupt();
    }

    protected override void UpdateEnemy(float deltaTime)
    {
        base.UpdateEnemy(deltaTime);
    }
}

