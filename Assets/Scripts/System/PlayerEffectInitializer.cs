using UnityEngine;

public class PlayerEffectInitializer : MonoBehaviour
{
    public void Init(Player player, SkillManager skillManager)
    {
        if (player == null)
        {
            Debug.LogError("[PlayerEffectInitializer] Player is null.", this);
            return;
        }

        //リーク対策
        _thunderEffectPresenter?.Dispose();
        _weaponEffectPresenter?.Dispose();
        _levelUpEffectPresenter?.Dispose();

        _thunderEffectPresenter = null;
        _weaponEffectPresenter = null;
        _levelUpEffectPresenter = null;

        if (skillManager != null && _levelUpEffectView != null)
        {
            _levelUpEffectPresenter = new LevelUpEffectPresenter(_levelUpEffectView, skillManager);
        }
        else
        {
            Debug.LogError("[PlayerEffectInitializer] SkillManager or LevelUpEffectView is missing.", this);
        }

        if (player.TryGetComponent(out IModeController modeController))
        {
            if (_thunderEffect != null)
            {
                _thunderEffectPresenter = new ThunderEffectPresenter(_thunderEffect, modeController);
            }
            else
            {
                Debug.LogError("[PlayerEffectInitializer] ThunderEffectView is missing.", this);
            }

            if (_thunderEffectView != null && _warriorEffectView != null)
            {
                _weaponEffectPresenter = new WeaponEffectPresenter(_thunderEffectView, _warriorEffectView, modeController);
            }
            else
            {
                Debug.LogError("[PlayerEffectInitializer] WeaponEffectView is missing.", this);
            }
        }
        else
        {
            Debug.LogError("[PlayerEffectInitializer] IModeController is missing.", this);
        }

        if (player.TryGetComponent(out PlayerEffectReceiver effectReceiver))
        {
            if (ServiceLocator.TryGet(out EffectManager effectManager))
            {
                effectReceiver.Init(player, effectManager);
            }
            else
            {
                Debug.LogError("[PlayerEffectInitializer] EffectManager is missing.", this);
            }
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
