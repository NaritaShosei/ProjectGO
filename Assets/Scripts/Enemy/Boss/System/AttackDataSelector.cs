using BossEnemy.Data;
using UnityEngine;

public class AttackDataSelector
{ 
    public static int GetRandamSelectAttackDataID(BossEnemyAttackField bossEnemyAttackField)
    {
        if (bossEnemyAttackField.AttackField == null || bossEnemyAttackField.AttackField.Length == 0) return default;

        // すべての攻撃の確率（重み）の合計を計算する
        float totalChance = 0f;
        foreach (var attack in bossEnemyAttackField.AttackField)
        {
            totalChance += attack.ActivationRate;
        }

        // 0 から 合計値 までの間でランダムな値を1つ取得
        float randomPoint = Random.Range(0f, totalChance);

        // 各攻撃の確率を足しながら、ランダム値を超えた瞬間の一手を決定
        float currentSum = 0f;
        foreach (var attack in bossEnemyAttackField.AttackField)
        {
            currentSum += attack.ActivationRate;
            if (randomPoint <= currentSum)
            {
                return attack.ID;
            }
        }

        // 基本的にはここには到達しませんが、安全のため最後の要素を返す
        return bossEnemyAttackField.AttackField[bossEnemyAttackField.AttackField.Length - 1].ID;
    }
}
