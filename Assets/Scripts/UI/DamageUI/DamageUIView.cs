using UnityEngine;

/// <summary>
/// ダメージUIのView
/// これも使用しない
/// </summary>
public class DamageUIView : MonoBehaviour
{
    [SerializeField] private DamageNuber _prefab;
    [SerializeField] private Transform _canvasRoot;

    /// <summary>
    /// 通常ダメージUI
    /// </summary>
    /// <param name="value"></param>
    /// <param name="worldPos"></param>
    public void ShowNomal(float value, Vector3 worldPos)
    {
        Create(value, worldPos, DamageType.Normal);
    }

    /// <summary>
    /// 弱点UI 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="worldPos"></param>
    public void ShowWeak(float value, Vector3 worldPos)
    {
        Create(value, worldPos, DamageType.Weak);
    }
    
    /// <summary>
    /// クリティカルUI
    /// </summary>
    /// <param name="value"></param>
    /// <param name="worldPos"></param>
    public void ShowCritical(float value, Vector3 worldPos)
    {
        Create(value, worldPos, DamageType.Critical);
    }

    private void Create(float value,Vector3 worldPos,DamageType type)
    {
        DamageNuber nuber = Instantiate(_prefab, _canvasRoot);
        nuber.Initialize(value, worldPos, type);
    }
}
