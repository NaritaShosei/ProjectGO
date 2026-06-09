using System;
using UnityEngine;

public class SequenceManager : MonoBehaviour
{
    #region パブリック

    /// <summary>全シークエンスクリア（リザルトへ）</summary>
    public event Action OnAllSequencesComplete;

    /// <summary>タイトルへ戻るリクエスト（ゲームオーバー後）</summary>
    public event Action OnTitleRequested;

    public bool IsAllSequencesComplete => _stateMachine == null;

    public void Init(EnemyManager enemyManager, SkillManager skillManager, InputHandler inputHandler, IPlayer player)
    {
        if (enemyManager == null || skillManager == null)
        {
            Debug.LogError("EnemyManager、SkillManagerが未設定です");
            enabled = false;
            return;
        }

        // コンテキスト構築
        _context = new SequenceStateContext
        {
            EnemyManager = enemyManager,
            SkillManager = skillManager,
            InputHandler = inputHandler,
            Player = player,
            SequenceManager = this,
            PhaseTimer = new CountDownTimer(),
            GameOverTimer = new CountDownTimer(),
            SkillSelectView = _skillUIManager,
            SkillSelectCount = _skillSelectCount,
            SpawnDataRepository = _spawnDataRepository,
            BossSpawnData = _bossSpawnData,
        };

        // プレイヤー死亡を購読
        if (player != null)
            player.OnDead += HandlePlayerDead;

        // StateMachine構築
        _stateMachine = new SequenceStateMachine(_context);
        RegisterStates();
    }

    /// <summary>シークエンスを開始する</summary>
    public void StartSequence()
    {
        _stateMachine?.Start(SequenceStateType.IntroMovie);
    }

    /// <summary>ResultStateから呼ばれる</summary>
    public void NotifyAllSequencesComplete()
    {
        OnAllSequencesComplete?.Invoke();
    }

    /// <summary>GameOverStateから呼ばれる</summary>
    public void NotifyTitleRequested()
    {
        OnTitleRequested?.Invoke();
    }

    #endregion

    #region　インスペクター
    
    [Header("スキル選択設定")]
    [SerializeField] private SkillSelectView _skillUIManager;
    [SerializeField] private int _skillSelectCount = 3;

    [Header("敵生成設定")]
    [SerializeField] private SpawnDataRepository _spawnDataRepository;
    [SerializeField] private SpawnData _bossSpawnData;

    #endregion

    #region フィールド変数

    private SequenceStateMachine _stateMachine;
    private SequenceStateContext _context;

    #endregion

    #region Unityイベント

    private void Update()
    {
        _stateMachine?.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (_context?.Player != null)
            _context.Player.OnDead -= HandlePlayerDead;

        _context?.PhaseTimer?.Dispose();
        _context?.GameOverTimer?.Dispose();
    }

    #endregion

    #region プライベートメソッド

    private void RegisterStates()
    {
        _stateMachine.RegisterState(new IntroMovieState());
        _stateMachine.RegisterState(new MobAndSkillState());
        _stateMachine.RegisterState(new BossIntroMovieState());
        _stateMachine.RegisterState(new BossBattleState());
        _stateMachine.RegisterState(new EndingMovieState());
        _stateMachine.RegisterState(new ResultState());
        _stateMachine.RegisterState(new GameOverState());
    }

    private void HandlePlayerDead()
    {
        // プレイヤー死亡 → ゲームオーバーへ強制遷移
        _context.IsPlayerDead = true;
        _stateMachine?.ForceTransition(SequenceStateType.GameOver);
    }

    #endregion
}
