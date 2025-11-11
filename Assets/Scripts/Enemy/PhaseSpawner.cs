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
    public GameObject enemyPrefab;            // スポーンする敵プレハブ
    public Transform playerTransform;         // プレイヤー（nullならワールド原点基準）
    public Transform[] spawnPoints;           // 指定スパーンポイントがある場合はこちらを優先
    public float spawnRadius = 8f;            // プレイヤー周りにランダムに出すときの半径

    [Header("Phase timing")]
    public float phaseDuration = 10f;         // フェーズ継続時間（秒） — 要件: 10秒
    public float initialDelay = 0.5f;         // 最初のフェーズ開始前の遅延

    [Header("Difficulty scaling")]
    public int baseEnemiesPerPhase = 1;       // フェーズ1あたりの基本スポーン数（フェーズ番号を掛ける）
    public float enemyCountMultiplier = 1.0f; // フェーズごとの増加係数（例: 1.2ならゆっくり増える）
    public float enemyHealthPerPhase = 1.0f;  // 敵の基本体力（フェーズで掛ける）

    [Header("Misc")]
    public bool autoStart = true;
    public bool useSpawnPoints = false;       // spawnPoints配列を優先するか
    public int maxPhases = 999;               // 無限にしないなら上限を

    [Header("Events")]
    public UnityEvent<int> OnPhaseStart;      // フェーズ開始時（引数: フェーズ番号）
    public UnityEvent<int> OnPhaseEnd;        // フェーズ終了時

    bool running = false;
    int currentPhase = 0;
    Coroutine phaseCoroutine;

    void Start()
    {
        if (autoStart) StartSpawning();
    }

    public void StartSpawning()
    {
        if (running) return;
        running = true;
        phaseCoroutine = StartCoroutine(PhaseLoop());
    }

    public void StopSpawning()
    {
        if (!running) return;
        running = false;
        if (phaseCoroutine != null) StopCoroutine(phaseCoroutine);
    }

    public void ResetSpawning()
    {
        StopSpawning();
        currentPhase = 0;
        StartSpawning();
    }

    IEnumerator PhaseLoop()
    {
        // optional initial delay
        yield return new WaitForSeconds(initialDelay);

        while (running && currentPhase < maxPhases)
        {
            currentPhase++;
            OnPhaseStart?.Invoke(currentPhase);

            // フェーズ中のスポーン（ここではフェーズ開始直後にスポーンして、フェーズ中は待つ）
            DoSpawnForPhase(currentPhase);

            // フェーズの経過を待つ
            yield return new WaitForSeconds(phaseDuration);

            OnPhaseEnd?.Invoke(currentPhase);
        }

        running = false;
    }

    void DoSpawnForPhase(int phase)
    {
        // 敵の数を決める（例: baseEnemiesPerPhase * phase ^ multiplier）
        // 誤魔化しなしに単純に計算
        float scaled = baseEnemiesPerPhase * Mathf.Pow(phase, enemyCountMultiplier);
        int spawnCount = Mathf.Max(1, Mathf.FloorToInt(scaled));

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 pos = ChooseSpawnPosition();
            var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
            ApplyPhaseToEnemy(go, phase, i);
        }
    }

    Vector3 ChooseSpawnPosition()
    {
        if (useSpawnPoints && spawnPoints != null && spawnPoints.Length > 0)
        {
            // ランダムにスパーンポイントを選ぶ
            var t = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return t.position;
        }

        // プレイヤー周りにランダムに出す（2Dなら z=0）
        Vector3 center = (playerTransform != null) ? playerTransform.position : Vector3.zero;
        // 円形ランダム（プレイヤーからspawnRadiusくらいの距離に出る）
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float r = Random.Range(spawnRadius * 0.5f, spawnRadius); // 中心寄りも出したければ0にする
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * r;
        return center + offset;
    }

    void ApplyPhaseToEnemy(GameObject enemyObj, int phase, int indexInPhase)
    {
        if (enemyObj == null) return;

        // 例: 敵のHealthコンポーネントやステータスを探して強化する
        // 下のは任意：敵プレハブが EnemyController というスクリプトを持っている想定
        var em = enemyObj.GetComponent<EnemyMove>();
        if (em != null)
        { 
            em._targetTransform = playerTransform;
        }
        var ec = enemyObj.GetComponent<EnemyController>();
        if (ec != null)
        {
            // 体力を増やす（実際のEnemyControllerに合わせて調整してね）
            ec.maxHealth *= (1f + (phase - 1) * enemyHealthPerPhase * 0.1f);
            ec.currentHealth = ec.maxHealth;

            // スピードや攻撃力も変えたいならここで調整
            ec.moveSpeed *= 1f + (phase - 1) * 0.02f;
        }
    }

    // デバッグ用にInspectorからフェーズを進める
    [ContextMenu("Advance Phase (debug)")]
    void DebugAdvancePhase()
    {
        if (!running)
        {
            StartSpawning();
            return;
        }
        // フェーズループを止めて1フェーズ分スキップ的に処理する
        StopSpawning();
        currentPhase++;
        DoSpawnForPhase(currentPhase);
    }
}
