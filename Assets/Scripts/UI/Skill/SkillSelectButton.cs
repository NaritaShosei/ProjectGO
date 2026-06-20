using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Threading;

public class SkillSelectButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
// TODO: コントローラー対応用かな？今のところこれ有効にしてるとマウスクリック後もふよふよしちゃってるから一旦殺す後ほど何とかしよう。
// ISelectHandler,
// IDeselectHandler
{
    /// <summary> マウスカーソルが重なった際のイベント </summary>
    public event Action<int> OnHighlighted;

    public void Setup(SkillViewData viewData, Action onClick)
    {
        _skillId = viewData.Id; // スキルIDを保持しておく

        _nameText.text = viewData.Name;
        _explanationText.text = viewData.Explanation;
        _icon.sprite = viewData.Icon;

        _isSelected = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _selectButton.onClick.RemoveAllListeners();
        _selectButton.onClick.AddListener(() => ClickAnimation().Forget()); // クリックアニメーションを追加
        if (onClick != null)
        {
            _selectButton.onClick.AddListener(onClick.Invoke);
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHovered(); // マウスが乗ったとき
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnUnhovered(); // マウスが離れたとき
    }

    // TODO: コントローラー対応用かな？今のところこれ有効にしてるとマウスクリック後もふよふよしちゃってるから一旦殺す後ほど何とかしよう。
    // public void OnDeselect(BaseEventData eventData)
    // {
    //     OnDeselected(); // 方向キー等で選択が解除された時
    // }

    // public void OnSelect(BaseEventData eventData)
    // {
    //     OnSelected(); // 方向キー等で選択された時
    // }

    [Header("ボタン設定")]
    [SerializeField] private Button _selectButton;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _explanationText;
    [SerializeField] private Image _icon;

    [Header("ハイライトアニメーション設定")]
    [Tooltip("マウスが乗ったときの時の拡大率")]
    [SerializeField] private float _hoveredScale = 1.1f;
    [Tooltip("マウスが乗ったときの拡大率になるまでの時間")]
    [SerializeField] private float _hoveredDuration = 0.1f;
    [Tooltip("アニメーションによる拡大率")]
    [SerializeField] private float _animationScale = 1.2f;
    [Tooltip("ハイライトアニメーションの時間")]
    [SerializeField] private float _highlightDuration = 1f;

    [Header("クリックアニメーション設定")]
    [Tooltip("クリック時の拡大率")]
    [SerializeField] private float _clickScale = 1.5f;
    [Tooltip("クリックアニメーションの時間")]
    [SerializeField] private float _clickDuration = 0.1f;

    private int _skillId; // スキルIDを保持しておく
    private float _defaultScale; // デフォルトのスケールを保持しておく
    private bool _isSelected = false; // スキル選択済みか
    private CancellationTokenSource _cts;

    private void OnHovered()
    {
        if (_isSelected) return; // 選択済みの場合はハイライトしない

        Debug.Log("マウスが乗った");
        OnHighlighted?.Invoke(_skillId);

        _defaultScale = transform.localScale.x; // デフォルトのスケールを保持

        _cts = new CancellationTokenSource();
        Highlight(true, _cts.Token).Forget(); // ハイライトアニメーション開始
    }

    private void OnUnhovered()
    {
        if (_isSelected) return;

        Debug.Log("マウスが離れた");

        _cts?.Cancel(); // ハイライトアニメーション停止
        _cts?.Dispose();
        _cts = null;
        Highlight(false, default).Forget(); // ハイライトアニメーション終了
    }

    private void OnSelected()
    {
        if (_isSelected) return;

        Debug.Log("方向キー等で選択された");
        OnHighlighted?.Invoke(_skillId);

        _defaultScale = transform.localScale.x; // デフォルトのスケールを保持
        _cts = new CancellationTokenSource();
        Highlight(true, _cts.Token).Forget(); // ハイライトアニメーション開始
    }


    private void OnDeselected()
    {
        if (_isSelected) return;

        Debug.Log("方向キー等で選択が解除された");

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        Highlight(false, default).Forget(); // ハイライトアニメーション終了
    }

    private async UniTask Highlight(bool isHovered, CancellationToken token)
    {
        // ハイライトアニメーションの処理
        if (isHovered && !_isSelected) // マウスが乗ったとき、かつ選択されていない場合のみアニメーションを実行
        {
            // ハイライトアニメーションの開始処理
            float elapsed = 0f;
            while (elapsed < _hoveredDuration)
            {
                if (token.IsCancellationRequested) return;
                elapsed += Time.deltaTime;
                float t = elapsed / _hoveredDuration;
                float scale = Mathf.Lerp(_defaultScale, _hoveredScale, t);
                transform.localScale = new Vector3(scale, scale, 1f);

                await UniTask.Yield();
            }

            // ハイライトアニメーションのループ処理
            elapsed = 0f;
            while (!token.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float pingPongT = Mathf.PingPong(elapsed / _highlightDuration * 2f, 1f);
                float scale = Mathf.Lerp(_hoveredScale, _animationScale, pingPongT);
                transform.localScale = new Vector3(scale, scale, 1f);

                await UniTask.Yield();
            }
        }
        // ハイライトアニメーション終了時の処理
        else
        {
            float elapsed = 0f;
            float currentScale = transform.localScale.x;
            while (elapsed < _hoveredDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _hoveredDuration;
                float scale = Mathf.Lerp(currentScale, _defaultScale, t);
                transform.localScale = new Vector3(scale, scale, 1f);

                await UniTask.Yield();
            }
        }
    }

    private async UniTaskVoid ClickAnimation()
    {
        if (_isSelected) return; // すでに選択済みの場合はアニメーションを実行しない
        Debug.Log("クリックアニメーション開始");

        _isSelected = true; // スキルが選択されたことを記録
        _cts?.Cancel(); // ハイライトアニメーション停止
        _cts?.Dispose();
        _cts = null;

        float elapsed = 0f;
        float currentScale = transform.localScale.x;
        while (elapsed < _clickDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _clickDuration;
            float scale = Mathf.Lerp(currentScale, _clickScale, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            await UniTask.Yield();
        }

        // 元のスケールに戻す
        elapsed = 0f;
        while (elapsed < _clickDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _clickDuration;
            float scale = Mathf.Lerp(_clickScale, _defaultScale, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            await UniTask.Yield();
        }
    }
}
