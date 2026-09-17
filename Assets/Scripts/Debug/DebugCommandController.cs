using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;

/// <summary>
/// ビルド版でも有効なデバッグ用コマンドをまとめて管理する。
/// </summary>
public class DebugCommandController : MonoBehaviour
{
    private const int TitleTransitionRequiredPressCount = 3;
    private const float TitleTransitionPressIntervalLimit = 1f;

    private int _titleTransitionPressCount;
    private float _titleTransitionLastPressedTime;

    private void Update()
    {
        TickTitleTransition();
        TickMobBattleSkip();
    }

    #region タイトル遷移（右Ctrl3連打）

    private void TickTitleTransition()
    {
        if (!IsRightCtrlPressed())
        {
            return;
        }

        CountTitleTransitionPress();

        if (_titleTransitionPressCount >= TitleTransitionRequiredPressCount)
        {
            TransitionToTitle();
            ResetTitleTransitionPressCount();
        }
    }

    // 右Ctrlキーがこのフレームで押されたかを判定する
    private bool IsRightCtrlPressed()
    {
        return Keyboard.current?.rightCtrlKey.wasPressedThisFrame == true;
    }

    // 前回の入力から一定時間空いていたら連打とみなさずカウントをリセットする
    private void CountTitleTransitionPress()
    {
        if (Time.unscaledTime - _titleTransitionLastPressedTime > TitleTransitionPressIntervalLimit)
        {
            _titleTransitionPressCount = 0;
        }

        _titleTransitionPressCount++;
        _titleTransitionLastPressedTime = Time.unscaledTime;
    }

    // 連打カウントをリセットする
    private void ResetTitleTransitionPressCount()
    {
        _titleTransitionPressCount = 0;
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

    #endregion

    #region モブ戦スキップ（右Ctrl + 右Alt + B 同時押し）

    private void TickMobBattleSkip()
    {
        if (!IsMobBattleSkipPressed())
        {
            return;
        }

        RequestMobBattleSkip();
    }

    // 右Ctrl + 右Alt + B が同時に押されているか判定する
    private bool IsMobBattleSkipPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.rightCtrlKey.isPressed
            && keyboard.rightAltKey.isPressed
            && keyboard.bKey.isPressed;
    }

    // ServiceLocator経由でSequenceManagerを取得しモブ戦スキップを要求する
    private void RequestMobBattleSkip()
    {
        if (!ServiceLocator.TryGet(out SequenceManager sequenceManager))
        {
            Debug.LogWarning("SequenceManagerが見つかりません。", this);
            return;
        }

        sequenceManager.RequestDebugMobBattleSkip();
    }

    #endregion
}
