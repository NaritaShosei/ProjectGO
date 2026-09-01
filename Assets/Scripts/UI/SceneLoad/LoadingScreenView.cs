using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ロード画面の表示を管理するクラス
/// </summary>
public class LoadingScreenView : MonoBehaviour
{
    /// <summary>
    /// ロード画面を表示する
    /// </summary>
    /// <returns></returns>
    public async UniTask ShowAsync()
    {
        gameObject.SetActive(true);

        _loadingSpinner.SetActive(true);

        _canvasGroup.blocksRaycasts = true;
        SetProgressVisible(false);

        await FadeAsync(0f, 1f);

        SetProgressVisible(true);

        await UniTask.NextFrame();
    }

    /// <summary>
    /// ロード画面の進捗を設定する
    /// </summary>
    /// <param name="progress"></param>
    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        _progressBar.fillAmount = progress;
    }

    /// <summary>
    /// ロード画面を非表示にする
    /// </summary>
    /// <returns></returns>
    public async UniTask HideAsync()
    {
        SetProgressVisible(false);

        await FadeAsync(1f, 0f);

        _loadingSpinner.SetActive(false);
        _canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    [Header("ロード画面の設定")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _progressBar;
    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private GameObject _loadingSpinner;

    /// <summary>
    /// 表示の切り替え
    /// </summary>
    private void SetProgressVisible(bool visible)
    {
        _progressBar.enabled = visible;
    }

    /// <summary>
    /// ロード画面のフェードイン・フェードアウトを非同期で行う
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    private async UniTask FadeAsync(float from, float to)
    {
        var elapsed = 0f;
        _canvasGroup.alpha = from;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha =
                Mathf.Lerp(from, to, elapsed / _fadeDuration);

            await UniTask.Yield();
        }

        _canvasGroup.alpha = to;
    }
}
