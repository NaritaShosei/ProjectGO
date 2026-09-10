using UnityEngine;
using System;

namespace BossEnemy.SMB
{
    public class BossMoveFootstepSMB : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _sortedFootstepTimings = CreateSortedTimings();
            _currentLoop = Mathf.FloorToInt(stateInfo.normalizedTime);
            _nextFootstepIndex = 0;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_sortedFootstepTimings == null || _sortedFootstepTimings.Length == 0) return;

            int loop = Mathf.FloorToInt(stateInfo.normalizedTime);
            if (loop != _currentLoop)
            {
                _currentLoop = loop;
                _nextFootstepIndex = 0;
            }

            float elapsedSecondsInLoop = (stateInfo.normalizedTime - loop) * stateInfo.length;
            while (_nextFootstepIndex < _sortedFootstepTimings.Length &&
                   elapsedSecondsInLoop >= _sortedFootstepTimings[_nextFootstepIndex])
            {
                Sound.PlaySE(animator.gameObject, SoundCueNames.Boss.Footstep, CueSheetType.Boss);
                _nextFootstepIndex++;
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _currentLoop = 0;
            _nextFootstepIndex = 0;
            _sortedFootstepTimings = null;
        }

        [SerializeField] private float[] _footstepTimings = { 0.15f, 0.65f };

        private int _currentLoop = 0;
        private int _nextFootstepIndex = 0;
        private float[] _sortedFootstepTimings = null;

        private float[] CreateSortedTimings()
        {
            if (_footstepTimings == null || _footstepTimings.Length == 0) return null;

            var timings = (float[])_footstepTimings.Clone();
            Array.Sort(timings);
            return timings;
        }
    }
}
