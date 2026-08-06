using System.Collections.Generic;
using UnityEngine;

using BossEnemy.Character;

namespace BossEnemy.Attack
{
    public class AttackDataSelector
    {
        public static int GetRandamSelectAttackDataID(AttackSelectionPool selectionPool, List<int> coolTimeAttackList)
        {
            if (selectionPool.SelectionPool.Length == 0 || selectionPool.SelectionPool == null) return default;

            // すべての攻撃の確率（重み）の合計を計算する
            float totalChance = 0f;
            foreach (var attack in selectionPool.SelectionPool)
            {
                if (coolTimeAttackList.Contains(attack.ID)) continue;
                if (attack.ActivationRate <= 0f) continue;

                totalChance += attack.ActivationRate;
            }

            if (totalChance <= 0f) return default;

            // 0 から 合計値 までの間でランダムな値を1つ取得
            float randomPoint = Random.Range(0f, totalChance);

            // 各攻撃の確率を足しながら、ランダム値を超えた瞬間の一手を決定
            float currentSum = 0f;
            foreach (var attack in selectionPool.SelectionPool)
            {
                if (coolTimeAttackList.Contains(attack.ID)) continue;
                if (attack.ActivationRate <= 0f) continue;

                currentSum += attack.ActivationRate;
                if (randomPoint <= currentSum)
                {
                    return attack.ID;
                }
            }

            // 基本的にはここには到達しませんが、安全のため最後の要素を返す
            return default;
        }
    }
}

