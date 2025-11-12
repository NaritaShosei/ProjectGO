using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// フェーズ制の敵スポーンマネージャー
/// 10秒ごとにフェーズが進行し、フェーズに応じて敵をスポーンする
/// Inspectorでプレハブ・プレイヤー・スポーン範囲・係数などを調節可能
/// </summary>
public class PhaseSpawner : MonoBehaviour
{
    [Header("Spawn settings")]
    [SerializeField] private GameObject _enemyPrefab;            // スポーンする敵プレハブ
    [SerializeField] private Transform _playerTransform;         // プレイヤー（nullならワールド原点基準）
    [SerializeField] private Transform[] _spawnPoints;           // 指定スパーンポイントがある場合はこちらを優先
    [SerializeField] private float _spawnRadius = 8f;            // プレイヤー周りにランダムに出すときの半径

    [Header("Phase timing")]
    [SerializeField] private float _phaseDuration = 10f;         // フェーズ継続時間（秒） — 要件: 10秒
    [SerializeField] private float _initialDelay = 0.5f;         // 最初のフェーズ開始前の遅延

    [Header("Difficulty scaling")]
    [SerializeField] private int _baseEnemiesPerPhase = 1;       // フェーズ1あたりの基本スポーン数（フェーズ番号を掛ける）
    [SerializeField] private float _enemyCountMultiplier = 1.0f; // フェーズごとの増加係数（例: 1.2ならゆっくり増える）
    [SerializeField] private float _enemyHealthPerPhase = 1.0f;  // 敵の基本体力（フェーズで掛ける）

    [Header("Misc")]
    [SerializeField] private bool _autoStart = true;
    [SerializeField] private bool _useSpawnPoints = false;       // spawnPoints配列を優先するか
    [SerializeField] private int _maxPhases = 999;               // 無限にしないなら上限を

    [Header("Events")]
    [SerializeField] private UnityEvent<int> _onPhaseStart;      // フェーズ開始時（引数: フェーズ番号）
    [SerializeField] private UnityEvent<int> _onPhaseEnd;        // フェーズ終了時

    private bool _running = false;
    private int _currentPhase = 0;
    private Coroutine _phaseCoroutine;

    private void Start()
    {
        if (_autoStart) StartSpawning();
    }

    public void StartSpawning()
    {
        if (_running) return;
        _running = true;
        _phaseCoroutine = StartCoroutine(PhaseLoop());
    }

    public void StopSpawning()
    {
        if (!_running) return;
        _running = false;
        if (_phaseCoroutine != null) StopCoroutine(_phaseCoroutine);
    }

    public void ResetSpawning()
    {
        StopSpawning();
        _currentPhase = 0;
        StartSpawning();
    }

    private IEnumerator PhaseLoop()
    {
        // optional initial delay
        yield return new WaitForSeconds(_initialDelay);

        while (_running && _currentPhase < _maxPhases)
        {
            _currentPhase++;
            _onPhaseStart?.Invoke(_currentPhase);

            // フェーズ中のスポーン（ここではフェーズ開始直後にスポーンして、フェーズ中は待つ）
            DoSpawnForPhase(_currentPhase);

            // フェーズの経過を待つ
            yield return new WaitForSeconds(_phaseDuration);

            _onPhaseEnd?.Invoke(_currentPhase);
        }

        _running = false;
    }

    private void DoSpawnForPhase(int phase)
    {
        // 敵の数を決める（例: baseEnemiesPerPhase * phase ^ multiplier）
        // 誤魔化しなしに単純に計算
        float scaled = _baseEnemiesPerPhase * Mathf.Pow(phase, _enemyCountMultiplier);
        int spawnCount = Mathf.Max(1, Mathf.FloorToInt(scaled));

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 pos = ChooseSpawnPosition();
            var go = Instantiate(_enemyPrefab, pos, Quaternion.identity);
            ApplyPhaseToEnemy(go, phase, i);
        }
    }

    private Vector3 ChooseSpawnPosition()
    {
        if (_useSpawnPoints && _spawnPoints != null && _spawnPoints.Length > 0)
        {
            // ランダムにスパーンポイントを選ぶ
            var t = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
            return t.position;
        }

        // プレイヤー周りにランダムに出す（2Dなら z=0）
        Vector3 center = (_playerTransform != null) ? _playerTransform.position : Vector3.zero;
        // 円形ランダム（プレイヤーからspawnRadiusくらいの距離に出る）
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float r = Random.Range(_spawnRadius * 0.5f, _spawnRadius); // 中心寄りも出したければ0にする
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * r;
        return center + offset;
    }

    private void ApplyPhaseToEnemy(GameObject enemyObj, int phase, int indexInPhase)
    {
        if (enemyObj == null) return;

        // 例: 敵のHealthコンポーネントやステータスを探して強化する
        // 下のは任意：敵プレハブが EnemyController というスクリプトを持っている想定
        var em = enemyObj.GetComponent<EnemyMove>();
        if (em != null)
        { 
            em._targetTransform = _playerTransform;
        }
        var ec = enemyObj.GetComponent<EnemyController>();
        if (ec != null)
        {
            // 体力を増やす（実際のEnemyControllerに合わせて調整してね）
            ec.MaxHealth *= (1f + (phase - 1) * _enemyHealthPerPhase * 0.1f);
            ec.CurrentHealth = ec.MaxHealth;

            // スピードや攻撃力も変えたいならここで調整
            ec.MoveSpeed *= 1f + (phase - 1) * 0.02f;
        }
    }

    // デバッグ用にInspectorからフェーズを進める
    [ContextMenu("Advance Phase (debug)")]
    private void DebugAdvancePhase()
    {
        if (!_running)
        {
            StartSpawning();
            return;
        }
        // フェーズループを止めて1フェーズ分スキップ的に処理する
        StopSpawning();
        _currentPhase++;
        DoSpawnForPhase(_currentPhase);
    }
}
