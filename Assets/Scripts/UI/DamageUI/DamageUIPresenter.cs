using UnityEngine;

/// <summary>
/// Damageイベントの受け取り
/// 使用しない
/// </summary>
public class DamageUIPresenter : MonoBehaviour
{
    [SerializeField] private DamageUIView _view;

    private void OnEnable()
    {
        //被ダメージのイベント登録
    }
    private void OnDisable()
    {
        //被ダメージのイベントを解除
    }

    /// <summary>
    ///ダメージの受け取り
    /// </summary>
    private void OnDamageRecived(DamageType type, float value, Vector3 hitpoint)
    {
        switch (type)
        {
            case DamageType.Normal:
                _view.ShowNomal(value, hitpoint);
                break;

            case DamageType.Weak:
                _view.ShowWeak(value, hitpoint);
                break;

            case DamageType.Critical:
                _view.ShowCritical(value, hitpoint);
                break;
        }

    }
}
