using BossEnemy.Character;
using BossEnemy.Interface;
using Cysharp.Threading.Tasks;
using System;
using UniRx;

namespace BossEnemy.UI
{
    public class BossEnemyHPUIPresenter : IDisposable
    {
        public BossEnemyHPUIPresenter(IBossCharacterEntity bossCharacterEntity, IBossHPView bossHPUIView)
        {
            _bossCharacterEntity = bossCharacterEntity;
            _bossHPUIView = bossHPUIView;
        }

        public void Init()
        {
            _phaseChangeSubscription = _bossCharacterEntity.CurrentAction
                .SkipLatestValueOnSubscribe()
                .Subscribe(currentAction =>
            {
                if (currentAction == CharacterAction.PhaseChanging) 
                    _bossHPUIView.ChangeHPUI(
                    _bossCharacterEntity.CharacterCurrentStats.MaxHP,
                    _bossCharacterEntity.CharacterCurrentStats.PhaseNum);
            });

            _bossHPSubscription = _bossCharacterEntity.CurrentHP
                .SkipLatestValueOnSubscribe()
                .Subscribe(async hp =>
            {
                UniTask takeDamageTask = _bossHPUIView.TakeDamage(hp);
                _bossHPUIView.SetRunningTask(takeDamageTask);
            });
        }

        public void Dispose()
        {
            // 個別に購読を解除
            _bossHPSubscription?.Dispose();
            _phaseChangeSubscription?.Dispose();
            _bossHPSubscription = null;
            _phaseChangeSubscription = null;
        }

        private IBossCharacterEntity _bossCharacterEntity = null;

        private IBossHPView _bossHPUIView = null;

        private IDisposable _bossHPSubscription;
        private IDisposable _phaseChangeSubscription;
    }
}
