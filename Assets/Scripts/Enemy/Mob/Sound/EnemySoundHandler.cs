using UnityEngine;

public class EnemySoundHandler : MonoBehaviour
{
    private Enemy _enemy;

    public void Init(Enemy enemy)
    {
        _enemy = enemy;

        _enemy.OnArmorBroken += HandleArmorBreak;
        _enemy.OnDead += HandleDead;
        _enemy.OnDamaged += HandleDamaged;
        _enemy.EnemyAnimator.OnAttackEffect += HandleAttack;
        _enemy.EnemyAnimator.OnDownStart += HandleDown;
        _enemy.EnemyAnimator.OnFootstep += HandleFootstep;
        _enemy.EnemyAnimator.OnBarkStart += HandleBark;
    }

    private void OnDestroy()
    {
        if (_enemy == null) return;

        _enemy.OnArmorBroken -= HandleArmorBreak;
        _enemy.OnDead -= HandleDead;
        _enemy.OnDamaged -= HandleDamaged;
        _enemy.EnemyAnimator.OnAttackEffect -= HandleAttack;
        _enemy.EnemyAnimator.OnDownStart -= HandleDown;
        _enemy.EnemyAnimator.OnFootstep -= HandleFootstep;
        _enemy.EnemyAnimator.OnBarkStart -= HandleBark;
    }

    private void HandleArmorBreak(IEnemy _)
    {
        Sound.PlaySE(
            gameObject,
            SoundCueNames.Common.ArmorBreak,
            CueSheetType.Common);
    }

    private void HandleDead(IEnemy _)
    {
        switch (_enemy.EnemyType)
        {
            case EnemyType.Draugr:
                Sound.PlaySE(
                    gameObject,
                    SoundCueNames.Common.EnemyFinisher,
                    CueSheetType.Common);
                break;

            case EnemyType.StoneGolem:
                Sound.PlaySE(
                    gameObject,
                    SoundCueNames.Common.EnemyFinisher,
                    CueSheetType.Common);

                Sound.PlaySE(
                    gameObject,
                    SoundCueNames.Enemy.StoneGolemDeathVoice,
                    CueSheetType.Golem);
                break;

        }

    }

    private void HandleDamaged(IEnemy _)
    {
        switch (_enemy.EnemyType)
        {
            case EnemyType.Draugr:
                Sound.PlaySE(gameObject, SoundCueNames.Enemy.DraugrDamageVoice, CueSheetType.Mob);
                break;
        }
    }

    private void HandleAttack()
    {
        switch (_enemy.EnemyType)
        {
            case EnemyType.Draugr:
                Sound.PlaySE(gameObject, SoundCueNames.Enemy.DraugrAttackVoice, CueSheetType.Mob);
                Sound.PlaySE(gameObject, SoundCueNames.Enemy.DraugrWeaponSwing, CueSheetType.Mob);
                break;
            case EnemyType.StoneGolem:
                Sound.PlaySE(gameObject, SoundCueNames.Enemy.StoneGolemFootStomp, CueSheetType.Golem);
                break;
        }
    }

    private void HandleBark()
    {
        switch (_enemy.EnemyType)
        {
            case EnemyType.Draugr:
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
        if (_enemy.EnemyType != EnemyType.StoneGolem) return;
        Sound.PlaySE(
            gameObject,
            SoundCueNames.Enemy.StoneGolemDownVoice,
            CueSheetType.Golem);
    }

    private void HandleFootstep()
    {
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
