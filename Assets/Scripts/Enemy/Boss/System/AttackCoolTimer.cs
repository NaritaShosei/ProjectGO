using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AttackCoolTimer
{
    public List<int> AttackCoolTimeList => _coolTimeList;

    public async UniTask StartCoolTime(int id, float coolTime = 0)
    {
        if (_coolTimeList.Contains(id)) return;

        _coolTimeList.Add(id);

        // coolTimeが0の場合時間を設けず好きなタイミングで手動解放させる
        if (coolTime == 0) return;

        await UniTask.Delay(TimeSpan.FromSeconds(3), delayTiming: PlayerLoopTiming.Update);

        _coolTimeList.Remove(id);
    }

    public void FinishCoolTime(int id)
    {
        _coolTimeList.Remove(id);
    }

    private List<int> _coolTimeList  = new();
}
