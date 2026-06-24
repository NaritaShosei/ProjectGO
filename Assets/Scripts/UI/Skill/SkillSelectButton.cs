using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Threading;

public class SkillSelectButton : MonoBehaviour,
    IPointerEnterHandler
// TODO: コントローラー対応用かな？今のところこれ有効にしてるとマウスクリック後もふよふよしちゃってるから一旦殺す後ほど何とかしよう。
// ISelectHandler,
// IDeselectHandler
{
    /// <summary> マウスカーソルが重なった際のイベント </summary>
    public event Action<int> OnHighlighted;

    public void Setup(SkillViewData viewData, Action onClick)
    {
        _skillId = viewData.Id;

        _nameText.text = viewData.Name;
        _explanationText.text = viewData.Explanation;
        _icon.sprite = viewData.Icon;

        _isSelected = false;
        DisposeCts();

        if (onClick == null)
        {
            Debug.LogError($"{nameof(onClick)}がnullです");
            return;
        }

        _selectButton.onClick.RemoveAllListeners();
        _selectButton.onClick.AddListener(() => ClickAnimation(onClick).Forget());
    }

    public void OnPointerEnter(PointerEventData eventData) => OnHovered();

    /// <summary> 外部からハイライトを開始する（初期選択など） </summary>
    public void ForceHighlight() => OnHovered();

    /// <summary> 外部からハイライトを停止する </summary>
    public void ForceUnhighlight() => StopHighlight("別のスキルに選択が移った");

    /// <summary>　外部からスキル選択アニメーションを再生する　/// </summary>
    public void SkillSelection(Action onClick) => ClickAnimation(onClick).Forget();

    // TODO: コントローラー対応用かな？今のところこれ有効にしてるとマウスクリック後もふよふよしちゃってるから一旦殺す後ほど何とかしよう。
    // public void OnSelect(BaseEventData eventData) => OnSelected();
    // public void OnDeselect(BaseEventData eventData) => OnDeselected();

    [Header("ボタン設定")]
    [SerializeField] private Button _selectButton;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _explanationText;
    [SerializeField] private Image _icon;

    [Header("ハイライトアニメーション設定")]
    [Tooltip("マウスが乗ったときの拡大率")]
    [SerializeField] private float _hoveredScale = 1.1f;
    [Tooltip("マウスが乗ったときの拡大率になるまでの時間")]
    [SerializeField] private float _hoveredDuration = 0.1f;
    [Tooltip("ループアニメーションの最大拡大率")]
    [SerializeField] private float _animationScale = 1.2f;
    [Tooltip("ハイライトループアニメーションの周期")]
    [SerializeField] private float _highlightDuration = 1f;

    [Header("クリックアニメーション設定")]
    [Tooltip("クリック時の拡大率")]
    [SerializeField] private float _clickScale = 1.5f;
    [Tooltip("クリックアニメーションの時間")]
    [SerializeField] private float _clickDuration = 0.1f;

    private int _skillId;
    private float _defaultScale;
    private bool _isSelected = false;
    private CancellationTokenSource _cts;

    // -------------------------
    // ホバー / 選択 イベント
    // -------------------------

    private void OnHovered() => StartHighlight("マウスが乗った");
    private void OnSelected() => StartHighlight("方向キー等で選択された");
    private void OnDeselected() => StopHighlight("方向キー等で選択が解除された");

    private void StartHighlight(string debugLog)
    {
        if (_isSelected) return;

        Debug.Log(debugLog);
        OnHighlighted?.Invoke(_skillId);

        _defaultScale = transform.localScale.x;
        ResetCts();
        Highlight(true, _cts.Token).Forget();
    }

    private void StopHighlight(string debugLog)
    {
        if (_isSelected) return;

        Debug.Log(debugLog);
        DisposeCts();
        Highlight(false, default).Forget();
    }

    // -------------------------
    // アニメーション
    // -------------------------

    private async UniTask Highlight(bool isHovered, CancellationToken token)
    {
        if (isHovered)
        {
            // デフォルト → ホバースケールへ
            await AnimateScale(_defaultScale, _hoveredScale, _hoveredDuration, token);
            if (token.IsCancellationRequested) return;

            // ホバースケール ↔ アニメーションスケールをループ
            float elapsed = 0f;
            while (!token.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float pingPongT = Mathf.PingPong(elapsed / _highlightDuration * 2f, 1f);
                float scale = Mathf.Lerp(_hoveredScale, _animationScale, pingPongT);
                transform.localScale = new Vector3(scale, scale, 1f);
                await UniTask.Yield();
            }
        }
        else
        {
            // 現在のスケール → デフォルトへ戻す
            await AnimateScale(transform.localScale.x, _defaultScale, _hoveredDuration);
        }
    }

    private async UniTaskVoid ClickAnimation(Action onClick)
    {
        if (_isSelected) return;
        Debug.Log("クリックアニメーション開始");

        _isSelected = true;
        DisposeCts();

        float currentScale = transform.localScale.x;
        await AnimateScale(currentScale, _clickScale, _clickDuration);
        await AnimateScale(_clickScale, _defaultScale, _clickDuration);

        onClick?.Invoke();
    }

    /// <summary> fromからtoへ指定時間でスケールをアニメーション </summary>
    private async UniTask AnimateScale(float from, float to, float duration, CancellationToken token = default)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (token.IsCancellationRequested) return;
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(from, to, elapsed / duration);
            transform.localScale = new Vector3(scale, scale, 1f);
            await UniTask.Yield();
        }
        // 最後に目標値に揃える
        transform.localScale = new Vector3(to, to, 1f);
    }

    // -------------------------
    // CTS ユーティリティ
    // -------------------------

    /// <summary> CTSをキャンセル・破棄して新規作成 </summary>
    private void ResetCts()
    {
        DisposeCts();
        _cts = new CancellationTokenSource();
    }

    /// <summary> CTSをキャンセル・破棄してnullにする </summary>
    private void DisposeCts()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}