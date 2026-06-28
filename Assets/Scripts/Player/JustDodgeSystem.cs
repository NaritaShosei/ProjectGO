using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class JustDodgeSystem : MonoBehaviour
{
    public event Action OnJustDodgeSuccess;

    /// <summary>
    /// ジャスト回避受付中か
    /// </summary>
    public bool IsJustDodgeWindow { get; private set; }

    public void JustDodgeWindowStart()
    {
        // 前回の受付終了待機をキャンセル
        _windowCts?.Cancel();
        _windowCts?.Dispose();

        _windowCts = new CancellationTokenSource();

        IsJustDodgeWindow = true;

        WindowEndAsync(_windowCts.Token).Forget();
    }

    /// <summary>
    /// 攻撃を受けた際に呼ぶ
    /// </summary>
    public bool TryJustDodge()
    {
        if (!IsJustDodgeWindow)
        {
            return false;
        }

        OnJustDodgeSuccess?.Invoke();

        // 1回成功したら閉じる場合
        JustDodgeWindowEnd();

        return true;
    }

    [Header("ジャスト回避判定のフレーム")]
    [SerializeField, Min(1)] private int _justDodgeFrame = 15;

    private CancellationTokenSource _windowCts;

    private void JustDodgeWindowEnd()
    {
        IsJustDodgeWindow = false;

        _windowCts?.Cancel();
        _windowCts?.Dispose();
        _windowCts = null;
    }

    private async UniTaskVoid WindowEndAsync(CancellationToken token)
    {
        try
        {
            await UniTask.DelayFrame(_justDodgeFrame, cancellationToken: token);

            JustDodgeWindowEnd();
        }
        catch (OperationCanceledException)
        {
            // キャンセル時は何もしない
        }
    }

    private void OnDestroy()
    {
        _windowCts?.Cancel();
        _windowCts?.Dispose();
    }
}
