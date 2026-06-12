using UnityEditor;
using UnityEngine;

/// <summary>
/// マップの円形エリア情報を管理するクラス
/// スポーン範囲のクランプ処理もここで提供する
/// </summary>
public class MapManager : MonoBehaviour
{
    //マップの中心位置
    public Vector3 CenterPosition => _centerPosition;
    public float Radius => _radius;

    /// <summary>
    /// 指定した位置をマップの円形エリア内にクランプする
    /// 範囲内の位置を返す
    /// </summary>
    /// <param name="position">補正対象座標</param>
    /// <returns>マップ範囲内に収まる座標</returns>
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

    [SerializeField] private Vector3 _centerPosition;
    [SerializeField] private float _radius = 50f;

    private void Awake()
    {
        if (!ServiceLocator.IsRegistered<MapManager>())
        {
            ServiceLocator.Register(this);
        }

    }
    private void OnDestroy()
    {
        ServiceLocator.Unregister<MapManager>();
    }

    private void OnValidate()
    {
        if (_radius <= 0f)
        {
            Debug.LogWarning($"[MapManager] Radius must be positive. Resetting to 50f.");
            _radius = 50f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = Color.green;
        Handles.DrawWireDisc(
            _centerPosition,
            Vector3.up,
            _radius);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_centerPosition, 1f);
    }
#endif

}
