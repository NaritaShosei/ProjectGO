using UnityEngine;

public class PlayerEffectInitializer : MonoBehaviour
{
    public void Init(Player player, SkillManager skillManager)
    {
        //リーク対策
        _thunderEffectPresenter?.Dispose();
        _weaponEffectPresenter?.Dispose();
        _levelUpEffectPresenter?.Dispose();

        _thunderEffectPresenter = null;
        _weaponEffectPresenter = null;
        _levelUpEffectPresenter = null;

        if (skillManager != null)
        {
            _levelUpEffectPresenter = new LevelUpEffectPresenter(_levelUpEffectView, skillManager);
        }
        else
        {
            Debug.LogError("SkillManagerが見つかりませんでした。");
        }

        if (player.TryGetComponent(out IModeController modeController))
        {
            _thunderEffectPresenter = new ThunderEffectPresenter(_thunderEffect, modeController);
            _weaponEffectPresenter = new WeaponEffectPresenter(_thunderEffectView, _warriorEffectView, modeController);
        }
        else
        {
            Debug.LogError("PlayerにIModeControllerが見つかりませんでした。");
        }
    }

    [SerializeField] private ThunderEffectView _thunderEffect;
    [SerializeField] private WeaponEffectView _thunderEffectView;
    [SerializeField] private WeaponEffectView _warriorEffectView;
    [SerializeField] private LevelUpEffectView _levelUpEffectView;

    private ThunderEffectPresenter _thunderEffectPresenter;
    private WeaponEffectPresenter _weaponEffectPresenter;
    private LevelUpEffectPresenter _levelUpEffectPresenter;

    private void OnDestroy()
    {
        _thunderEffectPresenter?.Dispose();
        _weaponEffectPresenter?.Dispose();
        _levelUpEffectPresenter?.Dispose();
    }
}
