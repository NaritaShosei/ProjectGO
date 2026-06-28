using BossEnemy.SMB;
using UnityEngine;
namespace BossEnemy.SMB
{
    public class HeavyPunchSMB : AttackSMBBase
    {
        protected override string AttackStartVoiceCueName => SoundCueNames.Boss.ChargePunchVoice;

        protected override string AttackCueName => SoundCueNames.Boss.ChargePunch;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateEnter(animator, stateInfo, layerIndex);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateUpdate(animator, stateInfo, layerIndex);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);
        }
    }
}
