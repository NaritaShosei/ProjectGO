using UnityEngine;

public class PlayerEffectInitializer : MonoBehaviour
{
    public void Init(Player player)
    {
        //リーク対策
        _thunderEffectPresenter?.Dispose();
        _weaponEffectPresenter?.Dispose();
        _thunderEffectPresenter = null;
        _weaponEffectPresenter = null;

        if (player.TryGetComponent(out IModeController modeController))
        {
            _thunderEffectPresenter = new ThunderEffectPresenter(_thunderEffect, modeController);
            _weaponEffectPresenter = new WeaponEffectPresenter( _weaponEffectView, modeController);
        }
        else
        {
            Debug.LogError("PlayerにIModeControllerが見つかりませんでした。");
        }
    }

    [SerializeField] private ThunderEffectView _thunderEffect;
    [SerializeField] private WeaponEffectView _weaponEffectView;

    private ThunderEffectPresenter _thunderEffectPresenter;
    private WeaponEffectPresenter _weaponEffectPresenter;

    private void OnDestroy()
    {
        _thunderEffectPresenter?.Dispose();
        _weaponEffectPresenter?.Dispose();
    }
}
