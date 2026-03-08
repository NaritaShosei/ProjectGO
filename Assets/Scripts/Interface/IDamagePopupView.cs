using System;

public interface IDamagePopupView 
{
    public void ShowDamage(DamagePopupViewModel viewModel);
    public event Action<IDamagePopupView> OnRelease;
}

public interface IDamagePopupModel
{
    public event Action<DamagePopupViewModel> OnDamageHit;
}
