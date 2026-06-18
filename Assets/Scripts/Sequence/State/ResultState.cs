using System;
using UnityEngine;


/// <summary>
/// リザルトのState。
/// 現状は SequenceManager.OnAllSequencesComplete を発火して外部（GameManager）にシーン遷移を委ねる。
/// インゲーム内リザルトUIにするか別シーンにするかは後で決める。
/// </summary>
[Serializable]
public class ResultState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.Result;

    public void OnEnter(SequenceStateContext context)
    {
        context.InputHandler?.EnableInput(false);

        // GameManagerのハンドラ（HandleGameComplete）を呼ぶ
        context.SequenceManager?.NotifyAllSequencesComplete();
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime) => null;

    public void OnExit(SequenceStateContext context) { }
}
