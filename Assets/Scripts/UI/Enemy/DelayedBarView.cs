using UnityEngine;
using UnityEngine.UI;

public class DelayedBarView : MonoBehaviour
{
    /// <summary>
    /// 値の設定
    /// </summary>
    /// <param name="ratio"></param>
    public  void SetValue(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        _instantBar.fillAmount = ratio;
        _delayTimer = _delayTime;
    }

    public void ResetValue(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        _instantBar.fillAmount = ratio;
        _delayedBar.fillAmount = ratio;
    }

    [Header("Bar")]
    [SerializeField] private Image _instantBar;//即時反映
    [SerializeField] private Image _delayedBar;//遅延追従

    [Header("Setting")]
    [SerializeField] private float _delayTime = 0.3f;
    [SerializeField] private float _followSpeed = 1f;

    private float _delayTimer;

    private void Update()
    {
        if (_delayTimer > 0f)
        {
            _delayTimer -= Time.deltaTime;
            return;
        }
        _delayedBar.fillAmount = Mathf.MoveTowards(_delayedBar.fillAmount,
                                                   _instantBar.fillAmount,
                                                   _followSpeed * Time.deltaTime);
    }
}
