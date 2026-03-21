using System;
using UnityEngine;

/// <summary>
/// 雷神モード専用ゲージのデータ管理クラス。
/// Player.Update から Tick を呼び、消費・回復を制御する。
/// </summary>
public class ThunderGauge
{
    /// <summary> HP変化通知と同じ形式 (current, max) </summary>
    public event Action<float, float> OnChanged;

    /// <summary> ゲージが0になったとき発火 — 強制モード解除用 </summary>
    public event Action OnDepleted;

    public float Current { get; private set; }
    public float Max { get; private set; }

    /// <summary> 1以上あれば雷神モードを使用可能 </summary>
    public bool CanUse => Current > 0f;

    /// <summary> 消費速度 (毎秒) : デフォルトは3秒で空になる </summary>
    public float DrainPerSecond { get; set; } = 100f / 3f;

    /// <summary> 回復速度 (毎秒) : デフォルトは3秒で全回復 </summary>
    public float RecoverPerSecond { get; set; } = 100f / 3f;

    public ThunderGauge(float max = 100f)
    {
        Max = max;
        Current = max;
    }

    /// <summary>
    /// Player.Update から毎フレーム呼ぶ。
    /// isThunderMode = true のとき消費、false のとき回復。
    /// </summary>
    public void Tick(float deltaTime, bool isThunderMode)
    {
        float before = Current;

        if (isThunderMode)
        {
            Current = Mathf.Max(0f, Current - DrainPerSecond * deltaTime);

            // 枯渇した瞬間のみ OnDepleted を発火
            if (before > 0f && Current <= 0f)
            {
                OnChanged?.Invoke(Current, Max);
                OnDepleted?.Invoke();
                return;
            }
        }
        else
        {
            Current = Mathf.Min(Max, Current + RecoverPerSecond * deltaTime);
        }

        // 変化があったときのみ通知（毎フレーム発火によるUI負荷を抑える）
        if (!Mathf.Approximately(before, Current))
        {
            OnChanged?.Invoke(Current, Max);
        }
    }
}
