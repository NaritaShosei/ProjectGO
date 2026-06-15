using UnityEngine;

public class GolemEnemy : MobEnemy
{
    [SerializeField] private float _downDuration = 5f;

    [SerializeField] private Renderer[] _bodyRenderer;

    private BlinkEffect _blinkEffect;

    private bool _isDown;

    public override void Init()
    {
        base.Init();
        Renderer renderer = GetComponent<Renderer>();
        _blinkEffect = new BlinkEffect(_bodyRenderer);
        Debug.Log(_bodyRenderer.Length);
        OnArmorBroken += HandleArmorBroken;
    }

    protected override void OnDestroy()
    {
        OnArmorBroken -= HandleArmorBroken;

        base.OnDestroy();
    }

    private void HandleArmorBroken(IEnemy enemy)
    {
        Debug.Log(_defenceContext.EnemyType);
        _isDown = true;

        _blinkEffect.StartBlink();

        ConditionController.ApplyCondition(
            new DownCondition(_downDuration));
    }

    public void RecoverArmor()
    {
        _blinkEffect.StopBlink();
        _isDown = false;

        if (_armor == null)
        {
            Debug.LogWarning("Armor is null.");
            return;
        }

        _armor.Restore();
        RebindArmor();
        _defenceContext.EnemyType = EnemyDefenceType.Armor;
    }

    public override void TakeDamage(DamageContext context)
    {
        if (_isDead)
        {
            return;
        }

        int damage =
            DamageSystem.CalculateDamage(
                context,
                _defenceContext);

        int showDamage = damage;

        if (!_isDown)
        {
            bool armorWasAlive =
    _defenceContext.EnemyType == EnemyDefenceType.Armor;

            _armor.AbsorbDamageAndReturnExcess(damage);

            bool isArmorBreak =
    armorWasAlive &&
    _defenceContext.EnemyType == EnemyDefenceType.Flesh;
            bool isWeak = context.PlayerMode == PlayerMode.Warrior;

            InvokeOnDamageDealt(
                showDamage,
                isWeak,
                context.IsCritical);

            InvokeOnHitEffect(
        new HitEffectContext
        {
            Position = transform.position,
            PlayerMode = context.PlayerMode,
            IsArmorHit = !isArmorBreak,
            IsArmorBreak = isArmorBreak
        });

            context.OnHitResult?.Invoke(
                new HitResult
                {
                    IsKill = false,
                    IsArmorBreak = isArmorBreak,
                    IsWeakPoint = isWeak,
                    IsArmorHit = !isArmorBreak,
                });

            if (!isArmorBreak)
            {
                InvokeOnDamaged();
            }
            return;
        }

        _stats.TakeDamage(damage);

        bool willKill = _stats.CurrentHealth <= 0;

        bool isWeakPoint = context.PlayerMode == PlayerMode.Thunder;

        InvokeOnDamageDealt(
            showDamage,
            isWeakPoint,
            context.IsCritical);

        context.OnHitResult?.Invoke(
            new HitResult
            {
                IsKill = willKill,
                IsArmorBreak = false,
                IsWeakPoint = isWeakPoint,
                IsArmorHit = false,
            });

        if (!willKill)
        {
            InvokeOnDamaged();
        }

        if (willKill)
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

