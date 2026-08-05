using UnityEngine;

namespace BossEnemy.SMB
{
    public class PunchSMB : AttackSMBBase
    {
        protected override string AttackStartVoiceCueName => SoundCueNames.Boss.HandSweepVoice;

        protected override string AttackCueName => SoundCueNames.Boss.HandSweep;

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
