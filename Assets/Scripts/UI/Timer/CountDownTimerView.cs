using UnityEngine;

public class CountDownTimerView : MonoBehaviour, IPhaseTimerView
{
    public void UpdateTimer(float current, float max)
    {
        if (_isDebug)
        {
            Debug.Log($"{name} => タイマー更新: {current}/{max}");
        }
        // タイマーのUIを更新する処理をここに実装
        // 例えば、スライダーやテキストを更新するなど
    }
    [Header("チェックするとデバッグログが表示されます")]
    [SerializeField] private bool _isDebug = false;
}
