using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BossEnemy.Model.Attack
{
    public class AttackCoolTimer
    {
        public List<int> AttackCoolTimeList => _coolTimeList;

        public async UniTask StartCoolTime(int id, float coolTime)
        {
            if (_coolTimeList.Contains(id)) return;

            _coolTimeList.Add(id);

            await UniTask.Delay(TimeSpan.FromSeconds(coolTime), delayTiming: PlayerLoopTiming.Update);

            _coolTimeList.Remove(id);
        }

        private List<int> _coolTimeList = new();
    }
}
