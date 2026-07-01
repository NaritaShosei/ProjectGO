using UnityEngine;

public class EnemySoundHandler : MonoBehaviour
{
    public void Init(Enemy enemy)
    {
        _enemy = enemy;

        _enemy.OnDead += HandleDead;
        _enemy.OnDamaged += HandleDamaged;
        _enemy.EnemyAnimator.OnAttackStart += HandleAttack;
        _enemy.EnemyAnimator.OnDownStart += HandleDown;
        _enemy.EnemyAnimator.OnFootstep += HandleFootstep;
        _enemy.EnemyAnimator.OnBarkStart += HandleBark;
        _enemy.EnemyAnimator.OnWeaponSwing += HandleWeaponSwing;
    }

    private Enemy _enemy;

    private void OnDestroy()
    {
        if (_enemy == null) return;

        _enemy.OnDead -= HandleDead;
        _enemy.OnDamaged -= HandleDamaged;
        _enemy.EnemyAnimator.OnAttackStart -= HandleAttack;
        _enemy.EnemyAnimator.OnDownStart -= HandleDown;
        _enemy.EnemyAnimator.OnFootstep -= HandleFootstep;
        _enemy.EnemyAnimator.OnBarkStart -= HandleBark;
        _enemy.EnemyAnimator.OnWeaponSwing -= HandleWeaponSwing;
    }

    private void HandleDead(IEnemy _)
    {
       
        switch (_enemy.EnemyType)
        {
            case EnemyType.StoneGolem:
                Sound.PlaySE(
                    gameObject,
                    SoundCueNames.Enemy.StoneGolemDeathVoice,
                    CueSheetType.Golem);
                break;

        }

    }

    private void HandleDamaged(IEnemy _)
    {
        Debug.Log("Dameg SE triggered");
        switch (_enemy.EnemyType)
        {
            case EnemyType.Draugr:
                Sound.PlaySE(gameObject, SoundCueNames.Enemy.DraugrDamageVoice, CueSheetType.Mob);
                break;
        }
    }

    private void HandleAttack()
    {
        Debug.Log("Attack SE triggered");
        switch (_enemy.EnemyType)
        {
            case EnemyType.Draugr:
                Sound.PlaySE(gameObject, SoundCueNames.Enemy.DraugrAttackVoice, CueSheetType.Mob);
                break;
        }
    }

    private void HandleWeaponSwing()
    {
        switch (_enemy.EnemyType)
        {
            case EnemyType.Draugr:
                Sound.PlaySE(
                    gameObject,
                    SoundCueNames.Enemy.DraugrWeaponSwing,
                    CueSheetType.Mob);
                break;

            case EnemyType.StoneGolem:
                Sound.PlaySE(
                    gameObject,
                    SoundCueNames.Enemy.StoneGolemGroundStomp,
                    CueSheetType.Golem);
                break;
        }
    }

    private void HandleBark()
    {
        Debug.Log("Bark SE triggered");
        switch (_enemy.EnemyType)
        {
            case EnemyType.Draugr:
                Debug.Log("Bark SE triggered2");
                Sound.PlaySE(
                    gameObject,
                    SoundCueNames.Enemy.DraugrBark,
                    CueSheetType.Mob);
                break;

            case EnemyType.StoneGolem:
                Sound.PlaySE(
                gameObject,
                    SoundCueNames.Enemy.StoneGolemBark,
                    CueSheetType.Golem);
                break;
        }
    }

    private void HandleDown()
    {
        Debug.Log("Down SE triggered");
        if (_enemy.EnemyType != EnemyType.StoneGolem) return;
        Sound.PlaySE(
            gameObject,
            SoundCueNames.Enemy.StoneGolemDownVoice,
            CueSheetType.Golem);
    }

    private void HandleFootstep()
    {
        Debug.Log("Step SE triggered");
        switch (_enemy.EnemyType)
        {
            case EnemyType.StoneGolem:
                Sound.PlaySE(
                    gameObject,
                    SoundCueNames.Enemy.StoneGolemFootstep,
                    CueSheetType.Golem);
                break;
        }
    }
}

//switchが多いく拡張がしずらいので後々、Profileのようなものを作ってEnemyごとに持たせるのがいいと思う。
