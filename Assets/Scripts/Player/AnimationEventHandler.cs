using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class AnimationEventHandler : MonoBehaviour
{
    [SerializeField] private SpeedManager _speedManager;
    [SerializeField] private PlayerManager _playerManager;
    private Slow _slow;

    private void Start()
    {
        _slow = _speedManager.Slow;
    }

    public async void OnSlowStart()
    {
        _slow.ChangeSlow(true);

        await UniTask.Delay(TimeSpan.FromSeconds(_slow.SlowDuration), cancellationToken: destroyCancellationToken);

        _slow.ChangeSlow(false);
        _playerManager.RemoveFlags(PlayerStateFlags.MoveLocked | PlayerStateFlags.ModeChange);
    }
}
