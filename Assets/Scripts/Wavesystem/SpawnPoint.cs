using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// エネミーの出現地点
/// SpawnSlotによるフォーメーション配置とエリアクランプを担当するクラス
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("SpawnPointSelector から参照するためのKey")]
    [SerializeField] private string _key;

    [Tooltip("エネミーを生成するローカル座標リスト（インスペクターで指定）")]
    [SerializeField] private List<Vector3> _spawnSlots = new();

    public string Key => _key;
    public int SlotCount => _spawnSlots.Count;

    /// <summary>
    /// 全Slotのワールド座標をクランプして返す
    /// </summary>
    public List<Vector3> GetWorldSlotPositions()
    {
        var result = new List<Vector3>(_spawnSlots.Count);
        foreach (var localSlot in _spawnSlots)
        {
            Vector3 worldPos = transform.TransformPoint(localSlot);
            Vector3 clamped = MapManager.Instance.ClampToArea(worldPos);
            result.Add(clamped);
        }
        return result;
    }
}
