using UnityEngine;

/// <summary>
/// マップの円形エリア情報を管理するクラス
/// スポーン範囲のクランプ処理もここで提供する
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [SerializeField] private Vector3 _centerPosition;
    [SerializeField] private float _radius = 50f;
    public Vector3 CenterPosition => _centerPosition;
    public float Radius => _radius;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("[MapManager] 複数のインスタンスが存在します");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public Vector3 ClampToArea(Vector3 position)
    {
        // Y軸は無視してXZ平面で判定
        Vector3 flat = new Vector3(position.x, 0f, position.z);
        Vector3 center = new Vector3(_centerPosition.x, 0f, _centerPosition.z);

        Vector3 offset = flat - center;
        if (offset.magnitude <= _radius)
            return position;

        // 円の境界内に補正（元のY座標を保持）
        Vector3 clamped = center + offset.normalized * _radius;
        return new Vector3(clamped.x, position.y, clamped.z);
    }

    /// <summary>
    /// 指定位置がマップの円形エリア内にあるか判定
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool IsInsideArea(Vector3 position)
    {
        Vector3 flat = new Vector3(position.x, 0f, position.z);
        Vector3 center = new Vector3(_centerPosition.x, 0f, _centerPosition.z);
        return (flat - center).magnitude <= _radius;
    }
}
