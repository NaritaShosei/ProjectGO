using UnityEngine;
using UnityEngine.UI;

public class LoadingBlinking : MonoBehaviour
{
    [SerializeField]
    private Image _image;

    [Header("アルファ値")]
    [SerializeField, Range(0f, 1f)]
    private float _minimumAlpha = 0.25f;

    [SerializeField, Range(0f, 1f)]
    private float _maximumAlpha = 1f;

    [Header("一往復にかかる秒数")]
    [SerializeField]
    private float _cycleDuration = 1.2f;

    private float _elapsedTime;

    private void Awake()
    {
        if (_image == null)
        {
            _image = GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        _elapsedTime = 0f;
        SetAlpha(_minimumAlpha);
    }

    private void Update()
    {
        if (_image == null)
            return;

        _elapsedTime += Time.unscaledDeltaTime;

        float duration =
            Mathf.Max(0.01f, _cycleDuration);

        float wave =
            (Mathf.Sin(
                (_elapsedTime / duration) *
                Mathf.PI * 2f -
                Mathf.PI * 0.5f) + 1f) * 0.5f;

        float alpha = Mathf.Lerp(
            _minimumAlpha,
            _maximumAlpha,
            wave);

        SetAlpha(alpha);
    }

    private void SetAlpha(float alpha)
    {
        if (_image == null)
            return;

        Color color = _image.color;
        color.a = Mathf.Clamp01(alpha);
        _image.color = color;
    }
}
