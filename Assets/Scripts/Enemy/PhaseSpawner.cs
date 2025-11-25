using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
[Serializable]
public struct PhaseSpawnSettings
{
    public EnemyBase Enemy;         // スポーンする敵プレハブ
    public Transform SpawnTransform;// スポーン位置
}

[Serializable]
public struct PhaseSettings
{
    public PhaseSpawnSettings[] SpawnSettings; // フェーズごとのスポーン設定
}

/// <summary>
/// フェーズ制の敵スポーンマネージャー
/// 外部から StartSpawning() を呼んで開始。敵が全滅するか時間経過で次フェーズへ移行する。
/// </summary>
public class PhaseSpawner : MonoBehaviour
{
    [Header("Spawn settings")]
    [SerializeField] private EnemyInstanceManager _enemyInstanceManager; // 敵インスタンス管理マネージャー
    [SerializeField] private PhaseSettings[] _phaseSettings;    // フェーズごとのスポーン設定
    [SerializeField] private Transform _playerTransform;    // プレイヤーのTransform（敵の移動初期化用）

    // フェーズ開始時にフェーズ番号(int)を渡す（1始まりにするか0始まりにするかは仕様で統一）
    public event Action<int> OnPhaseStart;
    public event Action<int> OnPhaseEnd;

    private bool _running = false;
    public bool IsRunning => _running;
    private int _currentPhaseIndex = 0; // 0-based index

    private CancellationTokenSource _cts;
    private void Start()
    {
        StartSpawning();
    }
    private void OnDisable()
    {
        // シーン切替や破棄時に確実に止める
        StopSpawning();
    }

    public void StartSpawning()
    {
        if (_running) return;
        _running = true;

        // 既存のCTSがあればキャンセルして破棄
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        // fire-and-forget でループを開始
        PhaseLoop(_cts.Token).Forget();
    }

    public void StopSpawning()
    {
        if (!_running) return;
        _running = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public void ResetSpawning()
    {
        StopSpawning();
        _currentPhaseIndex = 0;
        StartSpawning();
    }

    private async UniTaskVoid PhaseLoop(CancellationToken token)
    {
        // TODO:フェーズ開始前バフ選択などを待つ処理を追加する場合はここに実装
        // イベント待ち()

        while (_running && _currentPhaseIndex < _phaseSettings.Length && !token.IsCancellationRequested)
        {
            int phaseNumberForEvent = _currentPhaseIndex + 1; // 表示用は1始まりに
            OnPhaseStart?.Invoke(phaseNumberForEvent);

            // フェーズのスポーン処理
            var spawnSettings = _phaseSettings[_currentPhaseIndex].SpawnSettings;
            if (spawnSettings != null)
            {
                for (int i = 0; i < spawnSettings.Length; i++)
                {
                    var s = spawnSettings[i];
                    if (s.Enemy == null || s.SpawnTransform == null)
                    {
                        Debug.LogWarning($"Phase {_currentPhaseIndex} spawn[{i}] has null reference.");
                        continue;
                    }
                    _enemyInstanceManager.Spawn(s.Enemy, s.SpawnTransform,_playerTransform);
                }
            }

            // フェーズ終了条件：敵が全滅 を待つ
            var waitForDefeat = UniTask.WaitUntil(() => _enemyInstanceManager.AllEnemiesDefeated, cancellationToken: token);

            try
            {
                await UniTask.WhenAny(waitForDefeat);
            }
            catch (OperationCanceledException)
            {
                // キャンセルされたらループ抜け
                break;
            }

            OnPhaseEnd?.Invoke(phaseNumberForEvent);

            _currentPhaseIndex++;
        }

        _running = false;
    }

    // デバッグ用にInspectorからフェーズを開始
    [ContextMenu("Advance Phase (debug)")]
    private void DebugAdvancePhase()
    {
        if (!_running)
        {
            StartSpawning();
            return;
        }
    }
}
