using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Threading;

public class SkillSelectButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    /// <summary> マウスカーソルが重なった際のイベント </summary>
    public event Action<int> OnHighlighted;

    public void Setup(SkillViewData viewData, Action onClick)
    {
        _skillId = viewData.Id; // スキルIDを保持しておく

        _nameText.text = viewData.Name;
        _explanationText.text = viewData.Explanation;
        _icon.sprite = viewData.Icon;

        _selectButton.onClick.RemoveAllListeners();
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

    public void OnDeselect(BaseEventData eventData)
    {
        OnDeselected(); // 方向キー等で選択が解除された時
    }

    public void OnSelect(BaseEventData eventData)
    {
        OnSelected(); // 方向キー等で選択された時
    }

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

    private int _skillId; // スキルIDを保持しておく
    private float _defaultScale; // デフォルトのスケールを保持しておく
    private bool _isHovered; // マウスが乗っているかどうかの状態を保持しておく
    //private CancellationTokenSource _cts;

    private void OnHovered()
    {
        Debug.Log("マウスが乗った");
        OnHighlighted?.Invoke(_skillId);

        _defaultScale = transform.localScale.x; // デフォルトのスケールを保持
        _isHovered = true;
        Highlight(true).Forget(); // ハイライトアニメーション開始
    }

    private void OnUnhovered()
    {
        Debug.Log("マウスが離れた");

        _isHovered = false;
        Highlight(false).Forget(); // ハイライトアニメーション終了
    }

    private void OnSelected()
    {
        Debug.Log("方向キー等で選択された");
        OnHighlighted?.Invoke(_skillId);

        _defaultScale = transform.localScale.x; // デフォルトのスケールを保持
        _isHovered = true;
        Highlight(true).Forget(); // ハイライトアニメーション開始
    }


    private void OnDeselected()
    {

        Debug.Log("方向キー等で選択が解除された");

        _isHovered = false;
        Highlight(false).Forget(); // ハイライトアニメーション終了
    }

    private async UniTask Highlight(bool isHovered)
    {
        float scale = 0f;

        if (isHovered)
        {
            Debug.Log("ハイライトアニメーション開始");
            // ホバー状態への拡大アニメーション
            float elapsed = 0f;
            float defaultScale = _defaultScale; // 現在のスケールを基準にする
            while (elapsed < _hoveredDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _hoveredDuration;
                scale = Mathf.Lerp(defaultScale, _hoveredScale, t);

                transform.localScale = new Vector3(scale, scale, 1f);

                await UniTask.Yield(); // 1フレーム待機
            }

            // ハイライトアニメーション}
            elapsed = 0f;
            while (isHovered)
            {
                elapsed += Time.deltaTime;
                float pingPongT = Mathf.PingPong(elapsed / _highlightDuration * 2f, 1f); // 0から1までの値を往復させる
                scale = Mathf.Lerp(_hoveredScale, _animationScale, pingPongT);

                transform.localScale = new Vector3(scale, scale, 1f);

                await UniTask.Yield(); // 1フレーム待機
            }
        }
        else
        {
            Debug.Log("ハイライトアニメーション終了");
            // 元のスケールへの縮小アニメーション
            float elapsed = 0f;
            float currentScale = transform.localScale.x; // 現在のスケールを基準にする
            while (elapsed < _hoveredDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _hoveredDuration;
                scale = Mathf.Lerp(currentScale, _defaultScale, t);

                transform.localScale = new Vector3(scale, scale, 1f);

                await UniTask.Yield(); // 1フレーム待機
            }
        }
    }
}
