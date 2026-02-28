using UnityEngine;

/// <summary>
/// ダメージUIのView
/// </summary>
public class DamageUIView : MonoBehaviour
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Transform _canvasRoot;

    public void ShowNomal(float value, Vector3 worldPos)
    {
        Create(value, worldPos, DamageType.Normal);
    }

    public void ShowWeak(float value, Vector3 worldPos)
    {
        Create(value, worldPos, DamageType.Weak);
    }
    
    public void ShowCritical(float value, Vector3 worldPos)
    {
        Create(value, worldPos, DamageType.Critical);
    }

    private void Create(float value,Vector3 worldPos,DamageType type)
    {
        //DOTweenでUI生成
    }
}
