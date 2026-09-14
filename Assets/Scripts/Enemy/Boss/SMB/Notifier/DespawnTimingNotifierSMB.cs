using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class DespawnTimingNotifierSMB : BossCharacterSMB
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        DespawnAsync(_despawnTime).Forget();
    }

    [SerializeField] private float _despawnTime = 6.0f;

    private async UniTaskVoid DespawnAsync(float despawnTime)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(despawnTime));

        _animationEventReceiver.AnimEvent_Despawn();
    }
}

