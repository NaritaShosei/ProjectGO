using System;
using UnityEngine;

public class SequenceManager : MonoBehaviour
{
    public event Action OnAllSequencesComplete;
    public bool IsAllSequencesComplete => _currentSequenceIndex >= _sequenceDataBase.Sequences.Count;

    public void Init(EnemyManager enemyManager, SkillManager skillManager, InputHandler inputHandler, IPlayer player)
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
        _player = player;

        _battleTimer = new CountDownTimer();
        _skillSelectTimer = new CountDownTimer();

        _battleTimer.OnTimeEnded += () =>
        {
            Debug.LogWarning("バトルタイム切れ！ゲームオーバー");
        };

        _skillSelectTimer.OnTimeEnded += () =>
        {
            Debug.LogWarning("スキル選択タイム切れ！自動でスキルが選択されます");
        };

        InitializeContext();
    }

    public void StartSequence()
    {
        StartSequence(0);
        _battleTimer.StartTimer(_battleTimeLimit);
    }

    [Header("シークエンス設定")]
    [SerializeField] private SequenceDataBase _sequenceDataBase;
    [SerializeField] private int _skillSelectCount = 3;
    [SerializeField] private float _battleTimeLimit = 180f;
    [SerializeField] private float _bossBattleTimeLimit = 120f;
    [SerializeField] private float _skillSelectTimeLimit = 7f;

    [Header("敵生成設定")]
    [SerializeField] private SpawnDataRepository _spawnDataRepository;
    [SerializeField] private SpawnData _bossSpawnData;

    [Header("依存関係")]
    [SerializeField] private SkillSelectView _skillUIManager;

    private InputHandler _inputHandler;
    private SkillManager _skillManager;
    private EnemyManager _enemyManager;
    private SequenceBase _currentSequence;
    private IPlayer _player;
    private SequenceContext _context;
    private int _currentSequenceIndex = 0;
    private int _waveCount = 0;  // 何番目の雑魚敵シークエンスか

    private CountDownTimer _battleTimer;
    private CountDownTimer _skillSelectTimer;

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
            Player = _player
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

        // コンテキストのリセット
        _context.DefeatedCount = 0;

        // 雑魚敵シークエンスの場合、対応するSpawnDataを設定
        if (_currentSequence.SequenceType == SequenceType.Enemy)
        {
            _battleTimer.ResumeTimer();

            _context.CurrentSpawnData = GetSpawnData(_waveCount);
            _waveCount++;
        }

        // ボスシークエンスの場合、ボス用のSpawnDataを設定してタイマーをリセット
        else if (_currentSequence.SequenceType == SequenceType.Boss)
        {
            _battleTimer.StartTimer(_bossBattleTimeLimit);

            // ボスシークエンスは特定のSpawnDataを使うか、直接生成するか
            if (_bossSpawnData == null)
            {
                Debug.LogError("BossSpawnDataが未設定です");
                enabled = false;
                return;
            }

            _context.CurrentSpawnData = _bossSpawnData;
        }

        // スキル獲得シークエンスの場合、敵は出現させずにスキル選択タイマーを開始
        else
        {
            _skillSelectTimer.StartTimer(_skillSelectTimeLimit);
            _battleTimer.PauseTimer();
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

    private SpawnData GetSpawnData(int waveIndex)
    {
        var spawnDatas = _spawnDataRepository.SpawnDatas;

        if (waveIndex < spawnDatas.Length)
        {
            return spawnDatas[waveIndex];
        }

        return spawnDatas[UnityEngine.Random.Range(0, spawnDatas.Length)];
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
        _skillSelectTimer.StopTimer();
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
