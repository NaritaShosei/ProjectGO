using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpawnGroupの実行ごとに適切なSpawnPointを選択する
/// 除外ルール：プレイヤー近距離 / 直前使用済み
/// </summary>
public class SpawnPointSelector : MonoBehaviour
{
    /// <summary>
    /// Scene上の全SpawnPointを登録する
    /// WaveManagerのInit時に呼び出す
    /// </summary>
    public void Initialize()
    {
        _allSpawnPoints.Clear();

        var points = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        foreach (var point in points)
        {
            _allSpawnPoints.Add(point);
        }

        if (_allSpawnPoints.Count == 0)
            Debug.LogWarning("[SpawnPointSelector] SpawnPointがScene上に存在しません");
    }

    /// <summary>
    /// 条件を満たすSpawnPointを1つ選択して返す
    /// </summary>
    /// <param name="exclusionRadius">プレイヤーからの除外半径</param>
    /// <param name="requiredSlotCount">必要なSlot数（SpawnCountと一致確認）</param>
    public SpawnPoint Select(float exclusionRadius, int requiredSlotCount)
    {
        var candidates = BuildCandidates(exclusionRadius, requiredSlotCount);

        if (candidates.Count == 0)
        {
            // 条件を満たすSpawnPointがない場合は、除外ルールを緩和して再度検索したほうがいいかもしれない
            Debug.LogWarning("[SpawnPointSelector] 有効なSpawnPointが見つかりませんでした");
            return null;
        }

        var selected = candidates[Random.Range(0, candidates.Count)];
        _lastUsedPoint = selected;
        return selected;
    }

    [Tooltip("プレイヤーのTransform")]
    [SerializeField] private Transform _playerTransform;

    private SpawnPoint _lastUsedPoint;
    private readonly List<SpawnPoint> _allSpawnPoints = new();

    /// <summary>
    /// 除外ルールを適用して候補リストを構築する
    /// </summary>
    private List<SpawnPoint> BuildCandidates(float exclusionRadius, int requiredSlotCount)
    {
        var candidates = new List<SpawnPoint>();

        //除外ルールを適用して候補を絞り込む
        foreach (var point in _allSpawnPoints)
        {

            // 直前使用済みを除外
            if (point == _lastUsedPoint) continue;

            //// SlotCount不一致を除外
            //if (point.SlotCount < requiredSlotCount) continue;

            // プレイヤー近距離を除外
            if (IsNearPlayer(point, exclusionRadius)) continue;

            candidates.Add(point);
            //今のところは見つからない場合はnull返すだけだけど、除外ルールを緩和して再度検索するのもアリかも
        }

        return candidates;
    }

    /// <summary>
    /// SpawnPointがプレイヤーのExclusionRadius内にいるか判定
    /// </summary>
    private bool IsNearPlayer(SpawnPoint point, float exclusionRadius)
    {
        if (_playerTransform == null) return false;

        Vector3 pointFlat = new Vector3(point.transform.position.x, 0f, point.transform.position.z);
        Vector3 playerFlat = new Vector3(_playerTransform.position.x, 0f, _playerTransform.position.z);
        return (pointFlat - playerFlat).magnitude <= exclusionRadius;
    }
}
