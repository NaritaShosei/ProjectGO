using UnityEngine;

/// <summary>
/// 敵の攻撃パターンを定義するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "EnemyAttackPattern", menuName = "GameData/Enemy/EnemyAttackPattern")]
public sealed class EnemyAttackPattern : ScriptableObject
{
    public string PatternName;

    [Header("Slot")]
    // 攻撃時に占有するスロット数（1以上）
    [Min(1)]
    public int SlotCost = 1;

    [Header("Timing")]
    // 攻撃前の溜め時間
    [Min(0f)]
    public float WindUp = 0f;

    // 攻撃の持続時間
    [Min(0f)]
    public float Duration = 0.5f;

    // 攻撃後のクールダウン(もっと長いほうがいいかも）
    [Min(0f)]
    public float Cooldown = 1f;

    [Header("Hit")]
    // 攻撃中の最大ヒット数（Bossは複数Hit可）
    [Min(1)]
    public int MaxHitCount = 1;

    // 複数ヒット時のヒット間隔
    [Min(0f)]
    public float HitInterval = 0.2f;

    [Header("Knockback")]
    // ノックバックの強さ
    [Min(0f)]
    public float KnockbackPower;

    [Header("Damage")]
    // 基礎ダメージ量
    [Min(0)]
    public int BaseDamage;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // MaxHitCount > 1 のとき HitInterval が 0 だと
        // ヒット処理が瞬時に連続して意図しない挙動になる
        if (MaxHitCount > 1 && HitInterval <= 0f)
        {
            HitInterval = 0.1f;
            Debug.LogWarning(
                $"[EnemyAttackPattern] MaxHitCount > 1 のとき HitInterval は 0 より大きい必要があります。0.1 に補正しました。",
                this
            );
        }

        // HitInterval が Duration を超えると1回もヒットしない
        if (MaxHitCount > 1 && HitInterval > Duration)
        {
            HitInterval = Duration;
            Debug.LogWarning(
                $"[EnemyAttackPattern] HitInterval が Duration ({Duration}) を超えています。Duration に補正しました。",
                this
            );
        }
    }
#endif
}
