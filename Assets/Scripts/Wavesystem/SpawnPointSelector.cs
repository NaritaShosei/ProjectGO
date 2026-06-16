using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpawnGroupの実行ごとに適切なSpawnPointを選択する
/// 除外ルール：プレイヤー近距離 / 直前使用済み
/// </summary>
public class SpawnPointSelector : MonoBehaviour
{
    /// <summary>
    /// 条件を満たすSpawnPointを1つ選択して返す
    /// </summary>
    /// <param name="exclusionRadius">プレイヤーからの除外半径</param>
    /// <param name="requiredSlotCount">必要なSlot数（SpawnCountと一致確認）</param>
    public SpawnPoint Select(float exclusionRadius, string spawnPointKey = "")
    {
        // Key指定あり → 固定使用
        if (!string.IsNullOrEmpty(spawnPointKey))
        {
            return SelectByKey(spawnPointKey);
        }

        // Key指定なし → 自動選択
        return SelectAuto(exclusionRadius);
    }

    [Tooltip("プレイヤーのTransform")]
    [SerializeField] private Transform _playerTransform;

    [Tooltip("利用可能なSpawnPoint一覧")]
    [SerializeField] private List<SpawnPoint> _allSpawnPoints = new();

    private SpawnPoint _lastUsedPoint;
    private readonly Dictionary<string, SpawnPoint> _spawnPointMap = new();

    /// <summary>
    /// SpawnPointを初期化
    /// KeyがついているSpawnPointをDictionaryへ登録する
    /// </summary>
    private void Awake()
    {
        // 依存を注入する場合は外部公開メソッドで設定する形にしてもいいかも

        if (_playerTransform == null)
        {
            Debug.LogError("PlayerTransformが未設定です");
            return;
        }

        _spawnPointMap.Clear();

        foreach (var point in _allSpawnPoints)
        {
            if (point == null)
                continue;

            string key = point.Key;

            if (string.IsNullOrEmpty(key))
                continue;

            if (_spawnPointMap.ContainsKey(key))
            {
                Debug.LogWarning($"[SpawnPointSelector] 重複したKeyが存在します: {key}");
                continue;
            }

            _spawnPointMap.Add(key, point);
        }

        if (_allSpawnPoints.Count == 0)
            Debug.LogWarning("[SpawnPointSelector] SpawnPointが設定されていません");
    }


    /// <summary>
    /// Keyに対応するSpawnPointを返す
    /// </summary>
    private SpawnPoint SelectByKey(string key)
    {
        if (_spawnPointMap.TryGetValue(key, out var point))
        {
            _lastUsedPoint = point;
            return point;
        }

        Debug.LogError($"[SpawnPointSelector] Key '{key}' に対応するSpawnPointが見つかりません");
        return null;
    }

    /// <summary>
    /// 除外ルールを適用して自動選択する
    /// </summary>
    private SpawnPoint SelectAuto(float exclusionRadius)
    {
        var candidates = BuildCandidates(exclusionRadius);

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[SpawnPointSelector] 有効なSpawnPointが見つかりませんでした");
            return null;
        }

        var selected = candidates[Random.Range(0, candidates.Count)];
        _lastUsedPoint = selected;
        return selected;
    }


    /// <summary>
    /// 除外ルールを適用して候補リストを構築する
    /// </summary>
    private List<SpawnPoint> BuildCandidates(float exclusionRadius)
    {
        var candidates = new List<SpawnPoint>();

        //除外ルールを適用して候補を絞り込む
        foreach (var point in _allSpawnPoints)
        {

            // 直前使用済みを除外
            if (point == _lastUsedPoint) continue;

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
