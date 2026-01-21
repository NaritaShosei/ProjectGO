using UnityEngine;

public class SequenceManager : MonoBehaviour
{
    public bool IsAllPhasesComplete => _currentPhaseIndex >= _phases.Length;

    [Header("フェーズ設定")]
    [SerializeField] private SequenceBase[] _phases;

    [Header("敵生成設定")]
    [SerializeField] private SpawnDataRepository _spawnDataRepository;
    [SerializeField] private SpawnData _bossSpawnData;

    [Header("依存関係")]
    [SerializeField] private EnemyManager _enemyManager;
    // [SerializeField] private SkillUIManager _skillUIManager;

    private int _currentPhaseIndex = 0;
    private int _enemyPhaseCount = 0;  // 何番目の雑魚敵フェーズか
    private SequenceBase _currentPhase;
    private PhaseContext _context;
    private float _phaseStartTime;

    private void Start()
    {
        if (_spawnDataRepository == null || _spawnDataRepository.SpawnDatas == null)
        {
            Debug.LogError("SpawnDataRepositoryが未設定です");
            enabled = false;
            return;
        }

        InitializeContext();
        StartPhase(0);
    }

    private void Update()
    {
        if (_currentPhase == null || IsAllPhasesComplete) return;

        UpdateContext();
        _currentPhase.OnPhaseUpdate(_context);

        if (_currentPhase.IsComplete(_context))
        {
            NextPhase();
        }
    }

    private void InitializeContext()
    {
        _context = new PhaseContext
        {
            EnemyManager = _enemyManager,
            // SkillUIManager = _skillUIManager
        };

        // EnemyManagerのイベント購読
        _enemyManager.OnEnemyDefeated += HandleEnemyDefeated;
        _enemyManager.OnBossDefeated += HandleBossDefeated;

        // SkillUIManagerのイベント購読
        //if (_skillUIManager != null)
        //{
        //    _skillUIManager.OnSkillSelected += HandleSkillSelected;
        //}
    }

    private void UpdateContext()
    {
        _context.RemainingEnemies = _enemyManager.GetEnemyCount();
        _context.ElapsedTime = Time.time - _phaseStartTime;
    }

    private void StartPhase(int phaseIndex)
    {
        if (phaseIndex >= _phases.Length)
        {
            OnAllPhasesComplete();
            return;
        }

        _currentPhaseIndex = phaseIndex;
        _currentPhase = _phases[phaseIndex];
        _phaseStartTime = Time.time;

        // コンテキストのリセット
        _context.ElapsedTime = 0f;
        _context.DefeatedCount = 0;
        _context.SkillSelected = false;

        // 雑魚敵フェーズの場合、対応するSpawnDataを設定
        if (_currentPhase.PhaseType == PhaseType.Enemy)
        {
            if (_enemyPhaseCount < _spawnDataRepository.SpawnDatas.Length)
            {
                _context.CurrentSpawnData = _spawnDataRepository.SpawnDatas[_enemyPhaseCount];
                _enemyPhaseCount++;
            }
            else
            {
                Debug.LogWarning($"SpawnDataが不足しています。EnemyPhase: {_enemyPhaseCount}");
            }
        }
        else if (_currentPhase.PhaseType == PhaseType.Boss)
        {
            // ボスフェーズは特定のSpawnDataを使うか、直接生成するか
            if (_bossSpawnData == null)
            {
                Debug.LogError("BossSpawnDataが未設定です");
                enabled = false;
                return;
            }

            _context.CurrentSpawnData = _bossSpawnData;
        }
        else
        {
            _context.CurrentSpawnData = null;
        }

        Debug.Log($"フェーズ開始: {_currentPhase.PhaseType} (Phase {phaseIndex + 1})");

        _currentPhase.OnPhaseStart(_context);
    }

    private void NextPhase()
    {
        Debug.Log($"フェーズ完了: {_currentPhase.PhaseType}");
        StartPhase(_currentPhaseIndex + 1);
    }

    private void OnAllPhasesComplete()
    {
        Debug.Log("全フェーズクリア！");
        // ゲームクリア処理
    }

    private void HandleEnemyDefeated()
    {
        _context.DefeatedCount++;
    }

    private void HandleBossDefeated()
    {
        _context.BossDefeated = true;
    }

    private void HandleSkillSelected()
    {
        _context.SkillSelected = true;
    }

    private void OnDestroy()
    {
        if (_enemyManager != null)
        {
            _enemyManager.OnEnemyDefeated -= HandleEnemyDefeated;
            _enemyManager.OnBossDefeated -= HandleBossDefeated;
        }

        //if (_skillUIManager != null)
        //{
        //    _skillUIManager.OnSkillSelected -= HandleSkillSelected;
        //}
    }
}