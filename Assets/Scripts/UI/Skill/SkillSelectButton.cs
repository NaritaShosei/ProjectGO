using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// スキル選択画面に表示される1つ分のボタン。
/// このクラスは「クリックされた」「選択状態になった」という通知と、
/// 自分自身の見た目の演出だけを担当する。
/// 実際にどのボタンを選択状態にするか、他のボタンを止めるかは <see cref="SkillSelectView"/> 側で管理する。
/// </summary>
public class SkillSelectButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    /// <summary>
    /// このボタンがマウスホバー、またはEventSystem上で選択されたときに通知する。
    /// View側はこの通知を受けて、選択中ボタンの切り替えと演出制御を行う。
    /// </summary>
    public event Action<SkillSelectButton, int> OnHighlighted;

    /// <summary>
    /// このボタンがクリックされたときに通知する。
    /// ここではスキル獲得処理を直接行わず、View/Presenterへ処理を渡す。
    /// </summary>
    public event Action<SkillSelectButton, int> OnClicked;

    /// <summary> このボタンが表しているスキルID。 </summary>
    public int Id { get; private set; }

    /// <summary>
    /// 表示内容とクリックイベントを初期化する。
    /// 再表示時に前回の演出やクリック購読が残らないよう、必ず状態をリセットしてから設定する。
    /// </summary>
    public void Setup(SkillViewData viewData)
    {
        ResetState();

        Id = viewData.Id;
        _nameText.text = viewData.Name;
        _explanationText.text = viewData.Explanation;
        _icon.sprite = viewData.Icon;

        if (_selectButton == null)
        {
            Debug.LogError($"{nameof(SkillSelectButton)}のButton参照が設定されていません", this);
            return;
        }

        _selectButton.interactable = true;
        // UIButtonSoundなど、同じButtonに登録された他コンポーネントのリスナーは維持する。
        _selectButton.onClick.RemoveListener(RequestClick);
        _selectButton.onClick.AddListener(RequestClick);
    }

    /// <summary>
    /// マウスカーソルが乗ったとき、選択状態へ切り替えるための通知を出す。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        RequestHighlight();
    }

    /// <summary>
    /// キーボード/ゲームパッドなど、EventSystem経由で選択されたときの入口。
    /// マウス操作と同じ通知ルートに乗せる。
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        RequestHighlight();
    }

    /// <summary>
    /// 選択演出を強制停止する。
    /// Viewが別のボタンへ選択を移すときや、UIを閉じるときに呼ぶ。
    /// </summary>
    public void ForceStopHighlight()
    {
        SetHighlighted(false);
    }

    /// <summary>
    /// 選択中のループ演出を開始する。
    /// 複数ボタンが同時に再生されないよう、呼び出し元のView側で対象を1つに絞る。
    /// </summary>
    public void PlayHighlight()
    {
        SetHighlighted(true);
    }

    /// <summary>
    /// クリック演出を再生し、完了後にコールバックを呼ぶ。
    /// スキル獲得処理はこのコールバック経由でPresenterへ伝わる。
    /// </summary>
    public void PlayClick(Action onComplete)
    {
        ClickAnimation(onComplete).Forget();
    }

    /// <summary>
    /// UnityのButtonクリックイベントを実行する。
    /// 自動選択でも通常クリックと同じリスナーを通し、効果音などの付随処理を共通化する。
    /// </summary>
    public void PerformClick()
    {
        if (!isActiveAndEnabled || _selectButton == null || !_selectButton.IsInteractable())
        {
            return;
        }

        _selectButton.onClick.Invoke();
    }

    /// <summary>
    /// ボタンの内部状態とスケールを初期状態へ戻す。
    /// UIの再表示、非表示、未使用スロットの無効化時に使用する。
    /// </summary>
    public void ResetState()
    {
        _isClicking = false;
        SetHighlighted(false);
        ResetScale();
    }

    [Header("ボタン設定")]
    [SerializeField] private Button _selectButton;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _explanationText;
    [SerializeField] private Image _icon;

    [Header("選択演出設定")]
    [Tooltip("選択中の拡大率")]
    [SerializeField] private float _hoveredScale = 1.1f;
    [Tooltip("選択中の拡大率になるまでの時間")]
    [SerializeField] private float _hoveredDuration = 0.1f;
    [Tooltip("選択中ループ演出の最大拡大率")]
    [SerializeField] private float _animationScale = 1.2f;
    [Tooltip("選択中ループ演出の周期")]
    [SerializeField] private float _highlightDuration = 1f;

    [Header("クリック演出設定")]
    [Tooltip("クリック時の拡大率")]
    [SerializeField] private float _clickScale = 1.5f;
    [Tooltip("クリック演出の時間")]
    [SerializeField] private float _clickDuration = 0.1f;

    private float _baseScale = 1f;
    private bool _isHighlighted;
    private bool _isClicking;
    private CancellationTokenSource _highlightCts;
    private CancellationTokenSource _clickCts;

    /// <summary>
    /// Prefab上の初期スケールを記録する。
    /// 演出停止時はこの値に戻すため、Prefab側で大きさを変えてもコード修正が不要になる。
    /// </summary>
    private void Awake()
    {
        _baseScale = transform.localScale.x;
    }

    /// <summary>
    /// GameObjectが無効になったとき、進行中の演出を止めて見た目を戻す。
    /// 非同期演出が残って破棄済みオブジェクトに触る事故を防ぐ。
    /// </summary>
    private void OnDisable()
    {
        ResetState();
    }

    /// <summary>
    /// オブジェクト破棄時にCancellationTokenSourceを破棄する。
    /// UniTaskの未監視例外やリークを避けるための後始末。
    /// </summary>
    private void OnDestroy()
    {
        DisposeHighlightCts();
        DisposeClickCts();
    }

    /// <summary>
    /// 選択状態へ切り替えたいことをViewへ通知する。
    /// クリック演出中は選択変更を受け付けない。
    /// </summary>
    private void RequestHighlight()
    {
        if (!isActiveAndEnabled || _isClicking) return;

        OnHighlighted?.Invoke(this, Id);
    }

    /// <summary>
    /// クリックされたことをViewへ通知する。
    /// 連打でクリック演出やスキル獲得処理が二重に走らないよう、クリック演出中は無視する。
    /// </summary>
    private void RequestClick()
    {
        if (!isActiveAndEnabled || _isClicking) return;

        OnClicked?.Invoke(this, Id);
    }

    /// <summary>
    /// 選択演出の開始/停止を切り替える。
    /// すでに選択中の場合は演出を作り直さず、そのまま継続する。
    /// </summary>
    private void SetHighlighted(bool isHighlighted)
    {
        if (!isHighlighted)
        {
            DisposeHighlightCts();
            _isHighlighted = false;
            ResetScale();
            return;
        }

        if (_isHighlighted)
        {
            return;
        }

        DisposeHighlightCts();
        _isHighlighted = true;
        _highlightCts = new CancellationTokenSource();
        HighlightLoop(_highlightCts.Token).Forget();
    }

    /// <summary>
    /// 選択中の拡大ループ演出。
    /// CancellationTokenがキャンセルされるまで、ホバースケールと最大スケールの間を往復する。
    /// </summary>
    private async UniTaskVoid HighlightLoop(CancellationToken token)
    {
        await AnimateScale(transform.localScale.x, _hoveredScale, _hoveredDuration, token);
        if (token.IsCancellationRequested || !IsAlive()) return;

        float elapsed = 0f;
        while (!token.IsCancellationRequested && IsAlive())
        {
            elapsed += Time.deltaTime;
            float pingPongT = Mathf.PingPong(elapsed / _highlightDuration * 2f, 1f);
            float scale = Mathf.Lerp(_hoveredScale, _animationScale, pingPongT);
            transform.localScale = new Vector3(scale, scale, 1f);
            await UniTask.Yield();
        }
    }

    /// <summary>
    /// クリック時の拡大/復帰演出。
    /// 演出完了後にスキル選択完了のコールバックを呼ぶ。
    /// </summary>
    private async UniTaskVoid ClickAnimation(Action onComplete)
    {
        if (_isClicking || !isActiveAndEnabled) return;

        _isClicking = true;
        DisposeHighlightCts();
        DisposeClickCts();

        _clickCts = new CancellationTokenSource();
        var token = _clickCts.Token;

        float currentScale = transform.localScale.x;
        await AnimateScale(currentScale, _clickScale, _clickDuration, token);
        if (token.IsCancellationRequested || !IsAlive())
        {
            _isClicking = false;
            return;
        }

        await AnimateScale(_clickScale, _baseScale, _clickDuration, token);
        if (token.IsCancellationRequested || !IsAlive())
        {
            _isClicking = false;
            return;
        }

        _isClicking = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 指定したスケール値へ一定時間で補間する共通処理。
    /// キャンセルや破棄を検知したら、その場で安全に終了する。
    /// </summary>
    private async UniTask AnimateScale(float from, float to, float duration, CancellationToken token)
    {
        if (duration <= 0f)
        {
            if (IsAlive()) transform.localScale = new Vector3(to, to, 1f);
            return;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (token.IsCancellationRequested || !IsAlive()) return;

            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            transform.localScale = new Vector3(scale, scale, 1f);
            await UniTask.Yield();
        }

        if (IsAlive()) transform.localScale = new Vector3(to, to, 1f);
    }

    /// <summary>
    /// 見た目のスケールをPrefab上の初期値へ戻す。
    /// </summary>
    private void ResetScale()
    {
        if (!IsAlive()) return;

        transform.localScale = new Vector3(_baseScale, _baseScale, 1f);
    }

    /// <summary>
    /// Unityオブジェクトとしてまだ有効に参照できるかを確認する。
    /// 非同期処理中に破棄される可能性があるため、transformへ触る前に確認する。
    /// </summary>
    private bool IsAlive()
    {
        return this != null;
    }

    /// <summary>
    /// 選択演出用のCancellationTokenSourceをキャンセルして破棄する。
    /// </summary>
    private void DisposeHighlightCts()
    {
        _highlightCts?.Cancel();
        _highlightCts?.Dispose();
        _highlightCts = null;
    }

    /// <summary>
    /// クリック演出用のCancellationTokenSourceをキャンセルして破棄する。
    /// </summary>
    private void DisposeClickCts()
    {
        _clickCts?.Cancel();
        _clickCts?.Dispose();
        _clickCts = null;
    }
}
