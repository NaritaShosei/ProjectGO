using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;

/// <summary>
/// 右Ctrlキーを1秒以内に3回連続で押すとタイトルシーンへ遷移するデバッグコマンド
/// </summary>
public class DebugTitleTransitionCommand : MonoBehaviour
{
    private const int RequiredPressCount = 3;
    private const float PressIntervalLimit = 1f;

    private int _pressCount;
    private float _lastPressedTime;

    private void Update()
    {
        if (!IsRightCtrlPressed())
        {
            return;
        }

        CountPress();

        if (_pressCount >= RequiredPressCount)
        {
            TransitionToTitle();
            ResetPressCount();
        }
    }

    // 右Ctrlキーがこのフレームで押されたかを判定する
    private bool IsRightCtrlPressed()
    {
        return Keyboard.current?.rightCtrlKey.wasPressedThisFrame == true;
    }

    // 前回の入力から一定時間空いていたら連打とみなさずカウントをリセットする
    private void CountPress()
    {
        if (Time.unscaledTime - _lastPressedTime > PressIntervalLimit)
        {
            _pressCount = 0;
        }

        _pressCount++;
        _lastPressedTime = Time.unscaledTime;
    }

    // 連打カウントをリセットする
    private void ResetPressCount()
    {
        _pressCount = 0;
    }

    // ServiceLocator経由でSceneTransitionManagerを取得しタイトルへ遷移する
    private void TransitionToTitle()
    {
        if (!ServiceLocator.TryGet(out SceneTransitionManager transitionManager))
        {
            Debug.LogWarning("SceneTransitionManagerが見つかりません。", this);
            return;
        }

        transitionManager.TransitionToTitle().Forget();
    }
}
