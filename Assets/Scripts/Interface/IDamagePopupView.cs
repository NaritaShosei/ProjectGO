using System;

public interface IDamagePopupView 
{
    public void ShowDamage(DamagePopupViewModel viewModel);
    public event Action<IDamagePopupView> OnRelease;
}
