using System;
using UnityEngine;

public class SequenceManager : MonoBehaviour
{
    public event Action OnAllSequencesComplete;
    public bool IsAllSequencesComplete => _currentSequenceIndex >= _sequenceDataBase.Sequences.Count;

    public void Init(EnemyManager enemyManager,SkillManager skillManager,InputHandler inputHandler)
    {
        if (enemyManager == null || skillManager == null)
        {
            Debug.LogError("EnemyManager、SkillManagerが未設定です");
            enabled = false;
            return;
        }

        _enemyManager = enemyManager;
        _skillManager = skillManager;
        _inputHandler = inputHandler;
        InitializeContext();
    }

    public void StartSequence()
    {
        StartSequence(0);
    }

    [Header("シークエンス設定")]
    [SerializeField] private SequenceDataBase _sequenceDataBase;
    [SerializeField] private int _skillSelectCount = 3;

    [Header("敵生成設定")]
    [SerializeField] private SpawnDataRepository _spawnDataRepository;
    [SerializeField] private SpawnData _bossSpawnData;

    [Header("依存関係")]
    [SerializeField] private SkillSelectView _skillUIManager;

    private InputHandler _inputHandler;
    private SkillManager _skillManager;
    private EnemyManager _enemyManager;
    private int _currentSequenceIndex = 0;
    private int _enemySequenceCount = 0;  // 何番目の雑魚敵シークエンスか
    private SequenceBase _currentSequence;
    private SequenceContext _context;
    private float _sequenceStartTime;

    private void Start()
    {
        if (_spawnDataRepository == null || _spawnDataRepository.SpawnDatas == null)
        {
            Debug.LogError("SpawnDataRepositoryが未設定です");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (_currentSequence == null || IsAllSequencesComplete) return;

        UpdateContext();
        _currentSequence.OnSequenceUpdate(_context);

        if (_currentSequence.IsComplete(_context))
        {
            NextSequence();
        }
    }

    private void InitializeContext()
    {
        _context = new SequenceContext
        {
            EnemyManager = _enemyManager,
            SkillSelectView = _skillUIManager,
            SkillManager = _skillManager,
            SkillSelectCount = _skillSelectCount,
            InputHandler = _inputHandler,
        };

        // EnemyManagerのイベント購読
        _enemyManager.OnEnemyDefeated += HandleEnemyDefeated;
        _enemyManager.OnBossDefeated += HandleBossDefeated;

        // SkillUIManagerのイベント購読
        if (_skillUIManager != null)
        {
            _skillUIManager.OnSkillSelected += HandleSkillSelected;
        }
    }

    private void UpdateContext()
    {
        _context.RemainingEnemies = _enemyManager.GetEnemyCount();
        _context.ElapsedTime = Time.time - _sequenceStartTime;
    }

    private void StartSequence(int sequenceIndex)
    {
        if (sequenceIndex >= _sequenceDataBase.Sequences.Count)
        {
            OnAllSequenceComplete();
            _currentSequence = null;
            return;
        }

        _currentSequenceIndex = sequenceIndex;
        _currentSequence = _sequenceDataBase.Sequences[sequenceIndex];
        _sequenceStartTime = Time.time;

        // コンテキストのリセット
        _context.ElapsedTime = 0f;
        _context.DefeatedCount = 0;
        _context.SkillSelected = false;

        // 雑魚敵シークエンスの場合、対応するSpawnDataを設定
        if (_currentSequence.SequenceType == SequenceType.Enemy)
        {
            if (_enemySequenceCount < _spawnDataRepository.SpawnDatas.Length)
            {
                _context.CurrentSpawnData = _spawnDataRepository.SpawnDatas[_enemySequenceCount];
                _enemySequenceCount++;
            }
            else
            {
                Debug.LogWarning($"SpawnDataが不足しています。EnemySequence: {_enemySequenceCount}");
            }
        }
        else if (_currentSequence.SequenceType == SequenceType.Boss)
        {
            // ボスシークエンスは特定のSpawnDataを使うか、直接生成するか
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

        Debug.Log($"シークエンス開始: {_currentSequence.SequenceType} (Sequence {sequenceIndex + 1})");

        _currentSequence.OnSequenceStart(_context);
    }

    private void NextSequence()
    {
        Debug.Log($"シークエンス完了: {_currentSequence.SequenceType}");
        StartSequence(_currentSequenceIndex + 1);
    }

    private void OnAllSequenceComplete()
    {
        Debug.Log("全シークエンスクリア！");
        // ゲームクリア処理
        OnAllSequencesComplete?.Invoke();
    }

    private void HandleEnemyDefeated()
    {
        _context.DefeatedCount++;
    }

    private void HandleBossDefeated()
    {
        _context.BossDefeated = true;
    }

    private void HandleSkillSelected(int skillid)
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

        if (_skillUIManager != null)
        {
            _skillUIManager.OnSkillSelected -= HandleSkillSelected;
        }
    }
}