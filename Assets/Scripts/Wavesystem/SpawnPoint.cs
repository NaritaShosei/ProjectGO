using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// エネミーの出現地点
/// SpawnSlotによるフォーメーション配置を担当する。壁への補正は生成直前にEnemyManagerで行う。
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    public string Key => _key;

    /// <summary>
    /// 全Slotのワールド座標を取得する
    /// </summary>
    public List<Vector3> GetWorldSlotPositions()
    {
        var result = new List<Vector3>(_spawnSlots.Count);
        foreach (var localSlot in _spawnSlots)
        {
            Vector3 worldPos = transform.TransformPoint(localSlot);
            result.Add(worldPos);
        }
        return result;
    }

    /// <summary>
    /// 指定したSlotのワールド座標を取得する
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Vector3 GetSlotPosition(int index)
    {

        if (_spawnSlots.Count == 0)
        {
            return transform.position;
        }

        if (index < 0 || index >= _spawnSlots.Count)
        {
            return transform.position;
        }

        Vector3 worldPos =
            transform.TransformPoint(_spawnSlots[index]);

        return worldPos;
    }    

    [Tooltip("SpawnPointSelector から参照するためのKey")]
    [SerializeField] private string _key;

    [Tooltip("エネミーを生成するローカル座標リスト（インスペクターで指定）")]
    [SerializeField] private List<Vector3> _spawnSlots = new();

}
