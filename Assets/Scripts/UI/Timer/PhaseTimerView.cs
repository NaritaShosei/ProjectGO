using UnityEngine;

public class PhaseTimerView : MonoBehaviour, IPhaseTimerView
{
    public void UpdateTimer(float current, float max)
    {
        // タイマーのUIを更新する処理をここに実装
        // 例えば、スライダーやテキストを更新するなど
        Debug.Log($"{name} => タイマー更新: {current}/{max}");
    }
}
