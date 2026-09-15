using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ResultPanelView : MonoBehaviour
{
    public event Action TitleRequested;

    public int SMinimumScore => _sMinimumScore;
    public int AMinimumScore => _aMinimumScore;
    public int BMinimumScore => _bMinimumScore;

    public void SetScore(int value)
    {
        _scoreText = value.ToString("N0");
    }

    public void SetBossClearTime(string value)
    {
        _timeText = value;
    }

    public void SetLevel(string value)
    {
        _levelText = value;
    }

    public void SetRank(ResultRank rank)
    {
        if (_rankImage == null)
            return;

        float targetScale;

        switch (rank)
        {
            case ResultRank.S:
                _rankImage.sprite = _sRankSprite;
                targetScale = _sRankScale;
                break;

            case ResultRank.A:
                _rankImage.sprite = _aRankSprite;
                targetScale = _aRankScale;
                break;

            case ResultRank.B:
                _rankImage.sprite = _bRankSprite;
                targetScale = _bRankScale;
                break;

            default:
                _rankImage.sprite = _cRankSprite;
                targetScale = _cRankScale;
                break;
        }

        _rankTargetScale = new Vector3(
            targetScale,
            targetScale,
            1f);

        _rankImage.preserveAspect = true;
    }

    public void ShowPanel()
    {
        _resultSequence?.Kill();

        if (_root == null)
            return;

        _root.SetActive(true);
        Canvas.ForceUpdateCanvases();

        PrepareAnimation();
        CreateResultSequence();
    }

    public void HidePanel()
    {
        _resultSequence?.Kill();
        _resultSequence = null;

        if (_root != null)
            _root.SetActive(false);
    }



    [Header("Root")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _titleButton;

    [Header("各UIの位置")]
    [SerializeField] private RectTransform _background;
    [SerializeField] private RectTransform _title;
    [SerializeField] private RectTransform _rankRow;
    [SerializeField] private RectTransform _scoreRow;
    [SerializeField] private RectTransform _timeRow;
    [SerializeField] private RectTransform _levelRow;
    [SerializeField] private RectTransform _returnToTitleButton;

    [Header("各結果を表示するテキスト")]
    [SerializeField] private TextMeshProUGUI _scoreValue;
    [SerializeField] private TextMeshProUGUI _clearTimeValue;
    [SerializeField] private TextMeshProUGUI _levelValue;
    [SerializeField] private Image _rankImage;

    [Header("アニメーションの時間設定")]
    [SerializeField, Min(0f)] private float _moveDistance = 1200f;
    [SerializeField, Min(0.01f)] private float _backgroundMoveDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float _titleMoveDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float _rowMoveDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float _returnButtonMoveDuration = 0.25f;
    [SerializeField, Min(0f)] private float _moveInterval = 0.05f;
    [SerializeField] private Ease _moveEase = Ease.OutCubic;

    [Header("文字アニメーション")]
    [SerializeField, Min(0.01f)] private float _numberDuration = 0.7f;
    [SerializeField, Min(0f)] private float _numberInterval = 0.15f;

    [Header("ランク表示アニメーション")]
    [SerializeField, Min(1f)] private float _rankStartScaleMultiplier = 8f;
    [SerializeField, Min(0.01f)] private float _rankScaleDuration = 0.5f;
    [SerializeField] private Ease _rankScaleEase = Ease.OutCubic;


    [Header("Rank Score Thresholds (S >= A >= B)")]
    [Tooltip("このスコア以上でS。残り時間とレベルから算出したスコアで判定します。")]
    [SerializeField, Min(0)] private int _sMinimumScore = 25000;
    [SerializeField, Min(0)] private int _aMinimumScore = 20000;
    [Tooltip("このスコア未満はCになります。")]
    [SerializeField, Min(0)] private int _bMinimumScore = 15000;

    [Header("Rank Images")]
    [SerializeField] private Sprite _sRankSprite;
    [SerializeField] private Sprite _aRankSprite;
    [SerializeField] private Sprite _bRankSprite;
    [SerializeField] private Sprite _cRankSprite;

    [Header("Rank Sizes")]
    [SerializeField, Min(0.01f)] private float _sRankScale = 1.5f;
    [SerializeField, Min(0.01f)] private float _aRankScale = 1.3f;
    [SerializeField, Min(0.01f)] private float _bRankScale = 1.1f;
    [SerializeField, Min(0.01f)] private float _cRankScale = 0.9f;
    private Sequence _resultSequence;
    [SerializeField, Range(0f, 1f)]
    private float _numberStopAtNextMoveProgress = 0.1f;

    private Vector2 _backgroundTargetPosition;
    private Vector2 _titleTargetPosition;
    private Vector2 _rankRowTargetPosition;
    private Vector2 _scoreRowTargetPosition;
    private Vector2 _timeRowTargetPosition;
    private Vector2 _levelRowTargetPosition;
    private Vector2 _returnToTitleButtonTargetPosition;

    private Vector3 _rankTargetScale;

    private string _scoreText;
    private string _timeText;
    private string _levelText;

    private void Awake()
    {
        CacheTargetPositions();

        if (_titleButton != null)
            _titleButton.onClick.AddListener(HandleTitleButtonClicked);

        HidePanel();
    }

    private void OnValidate()
    {
        _bMinimumScore = Mathf.Max(0, _bMinimumScore);
        _aMinimumScore = Mathf.Max(_bMinimumScore, _aMinimumScore);
        _sMinimumScore = Mathf.Max(_aMinimumScore, _sMinimumScore);
    }

    private void OnDestroy()
    {
        _resultSequence?.Kill();

        if (_titleButton != null)
            _titleButton.onClick.RemoveListener(HandleTitleButtonClicked);
    }

    /// <summary>
    /// 各UIのターゲット位置を保存
    /// </summary>
    private void CacheTargetPositions()
    {
        if (_background != null)
            _backgroundTargetPosition = _background.anchoredPosition;

        if (_title != null)
            _titleTargetPosition = _title.anchoredPosition;

        if (_rankRow != null)
            _rankRowTargetPosition = _rankRow.anchoredPosition;

        if (_scoreRow != null)
            _scoreRowTargetPosition = _scoreRow.anchoredPosition;

        if (_timeRow != null)
            _timeRowTargetPosition = _timeRow.anchoredPosition;

        if (_levelRow != null)
            _levelRowTargetPosition = _levelRow.anchoredPosition;

        if (_returnToTitleButton != null)
        {
            _returnToTitleButtonTargetPosition =
                _returnToTitleButton.anchoredPosition;
        }
    }

    /// <summary>
    /// アニメーション開始前の初期状態を設定
    /// </summary>
    private void PrepareAnimation()
    {
        if (_titleButton != null)
            _titleButton.interactable = false;

        SetStartPosition(_background, _backgroundTargetPosition);
        SetStartPosition(_title, _titleTargetPosition);
        SetStartPosition(_rankRow, _rankRowTargetPosition);
        SetStartPosition(_scoreRow, _scoreRowTargetPosition);
        SetStartPosition(_timeRow, _timeRowTargetPosition);
        SetStartPosition(_levelRow, _levelRowTargetPosition);
        SetStartPosition(_returnToTitleButton,_returnToTitleButtonTargetPosition);

        if (_scoreValue != null)
            _scoreValue.text = string.Empty;

        if (_clearTimeValue != null)
            _clearTimeValue.text = string.Empty;

        if (_levelValue != null)
            _levelValue.text = string.Empty;

        if (_rankImage != null)
        {
            _rankImage.enabled = false;
            _rankImage.rectTransform.localScale =
                _rankTargetScale * _rankStartScaleMultiplier;
        }
    }

    /// <summary>
    /// 結果パネルのアニメーションシーケンス生成
    /// </summary>
    private void CreateResultSequence()
    {
        _resultSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);

        AppendMove(
            _background,
            _backgroundTargetPosition,
            _backgroundMoveDuration);

        AppendMove(
            _title,
            _titleTargetPosition,
            _titleMoveDuration);

        AppendMove(
            _rankRow,
            _rankRowTargetPosition,
            _rowMoveDuration);

        AppendMove(
            _scoreRow,
            _scoreRowTargetPosition,
            _rowMoveDuration);

        AppendNumberWithNextMove(
            _scoreValue,
            _scoreText,
            _timeRow,
            _timeRowTargetPosition,
            _rowMoveDuration);

        AppendNumberWithNextMove(
            _clearTimeValue,
            _timeText,
            _levelRow,
            _levelRowTargetPosition,
            _rowMoveDuration);

        AppendNumberWithNextMove(
            _levelValue,
            _levelText,
            _returnToTitleButton,
            _returnToTitleButtonTargetPosition,
            _returnButtonMoveDuration);


        AppendRankAnimation();

        _resultSequence.OnComplete(HandleAnimationCompleted);
    }

    /// <summary>
    /// 指定したUIを指定した位置まで移動させるアニメーションシーケンス
    /// </summary>
    /// <param name="target"></param>
    /// <param name="targetPosition"></param>
    /// <param name="duration"></param>
    private void AppendMove(
        RectTransform target,
        Vector2 targetPosition,
        float duration)
    {
        if (target == null)
            return;

        _resultSequence.Append(
            target
                .DOAnchorPos(targetPosition, duration)
                .SetEase(_moveEase));

        _resultSequence.AppendInterval(_moveInterval);
    }

    /// <summary>
    /// 指定したテキストを変化させるアニメーションシーケンス
    /// </summary>
    private void AppendNumberAnimation(
        TextMeshProUGUI text,
        string finalText)
    {
        if (text == null)
            return;

        float progress = 0f;

        _resultSequence.Append(
            DOTween.To(
                    () => progress,
                    value =>
                    {
                        progress = value;

                        text.text = progress >= 1f
                            ? finalText
                            : CreateScrambledText(finalText);
                    },
                    1f,
                    _numberDuration)
                .SetEase(Ease.Linear));

        _resultSequence.AppendInterval(_numberInterval);
    }

    /// <summary>
    /// ランク画像のスケールアニメーション
    /// </summary>
    private void AppendRankAnimation()
    {
        if (_rankImage == null)
            return;

        _resultSequence.AppendCallback(() =>
        {
            _rankImage.enabled = _rankImage.sprite != null;

            _rankImage.rectTransform.localScale =
                _rankTargetScale * _rankStartScaleMultiplier;
        });

        _resultSequence.Append(
            _rankImage.rectTransform
                .DOScale(_rankTargetScale, _rankScaleDuration)
                .SetEase(_rankScaleEase));
    }

    /// <summary>
    /// アニメーション完了時の処理
    /// </summary>
    private void HandleAnimationCompleted()
    {
        if (_titleButton == null)
            return;

        _titleButton.interactable = true;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(
                _titleButton.gameObject);
    }

    private void HandleTitleButtonClicked()
    {
        TitleRequested?.Invoke();
    }

    /// <summary>
    /// 指定したUIの開始位置を設定
    /// </summary>
    private void SetStartPosition(
        RectTransform target,
        Vector2 targetPosition)
    {
        if (target == null)
            return;

        target.anchoredPosition =
            targetPosition + Vector2.left * _moveDistance;
    }

    /// <summary>
    /// 指定した文字列の数字をランダムに入れ替えた文字列を生成
    /// </summary>
    private static string CreateScrambledText(string source)
    {
        if (string.IsNullOrEmpty(source))
            return string.Empty;

        char[] characters = source.ToCharArray();

        for (int i = 0; i < characters.Length; i++)
        {
            if (!char.IsDigit(characters[i]))
                continue;

            characters[i] =
                (char)('0' + UnityEngine.Random.Range(0, 10));
        }

        return new string(characters);
    }

    private void AppendNumberWithNextMove(
    TextMeshProUGUI text,
    string finalText,
    RectTransform nextPanel,
    Vector2 nextPanelTargetPosition,
    float nextPanelMoveDuration)
    {
        float numberStartTime = _resultSequence.Duration();

        _resultSequence.Append(
            CreateNumberTween(text, finalText));

        float overlapDuration =
            nextPanelMoveDuration * _numberStopAtNextMoveProgress;

        float nextPanelStartTime =
            numberStartTime +
            Mathf.Max(0f, _numberDuration - overlapDuration);

        _resultSequence.Insert(
            nextPanelStartTime,
            nextPanel
                .DOAnchorPos(
                    nextPanelTargetPosition,
                    nextPanelMoveDuration)
                .SetEase(_moveEase));

        _resultSequence.AppendInterval(_moveInterval);
    }

    private Tween CreateNumberTween(
        TextMeshProUGUI text,
        string finalText)
    {
        float progress = 0f;

        return DOTween.To(
                () => progress,
                value =>
                {
                    progress = value;

                    text.text = progress >= 1f
                        ? finalText
                        : CreateScrambledText(finalText);
                },
                1f,
                _numberDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => text.text = finalText);
    }
}
