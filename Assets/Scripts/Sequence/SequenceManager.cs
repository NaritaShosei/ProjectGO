using System;
using UnityEngine;

public class SequenceManager : MonoBehaviour
{
    #region パブリック

    /// <summary>全シークエンスクリア（リザルトへ）</summary>
    public event Action OnAllSequencesComplete;

    /// <summary>タイトルへ戻るリクエスト（ゲームオーバー後）</summary>
    public event Action OnTitleRequested;

    public void Init(EnemyManager enemyManager, SkillManager skillManager, InputHandler inputHandler, IPlayer player)
    {
        if (enemyManager == null || skillManager == null)
        {
            Debug.LogError("EnemyManager、SkillManagerが未設定です");
            enabled = false;
            return;
        }

        var phaseTimer = new CountDownTimer();
        var skillSelectTimer = new CountDownTimer();
        var gameOverTimer = new CountDownTimer();

        _battleTimerPresenter = null;
        _skillSelectTimerPresenter = null;
        _gameOverTimerPresenter = null;

        // タイマーUIのPresenterを必要に応じて生成。nullチェックしているので、InspectorでUIを割り当てなくても動作する。
        if (_battleTimerView != null)
            _battleTimerPresenter = new PhaseTimerPresenter(phaseTimer, _battleTimerView);
        if (_skillSelectTimerView != null)
            _skillSelectTimerPresenter = new PhaseTimerPresenter(skillSelectTimer, _skillSelectTimerView);
        if (_gameOverTimerView != null)
            _gameOverTimerPresenter = new PhaseTimerPresenter(gameOverTimer, _gameOverTimerView);

        // SpawnPointSelectorの初期化
        _spawnPointSelector.Initialize();

        // コンテキスト構築
        _context = new SequenceStateContext
        {
            EnemyManager = enemyManager,
            SkillManager = skillManager,
            InputHandler = inputHandler,
            Player = player,
            SequenceManager = this,
            PhaseTimer = phaseTimer,
            SkillSelectTimer = skillSelectTimer,
            GameOverTimer = gameOverTimer,
            MobBattleTimeLimit = _mobBattleTimeLimit,
            BossBattleTimeLimit = _bossBattleTimeLimit,
            SkillSelectTimeLimit = _skillSelectTimeLimit,
            SkillSelectView = _skillUIManager,
            SkillSelectCount = _skillSelectCount,
            WaveSequenceData = _waveSequenceData,
            SpawnPointSelector = _spawnPointSelector,
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
    [SerializeField] private WaveSequenceData _waveSequenceData;   
    [SerializeField] private SpawnPointSelector _spawnPointSelector; 
    [SerializeField] private SpawnData _bossSpawnData;

    [Header("タイマー設定")]
    [SerializeField] private float _mobBattleTimeLimit = 180f;
    [SerializeField] private float _bossBattleTimeLimit = 120f;
    [SerializeField] private float _skillSelectTimeLimit = 10f;

    [Header("タイマーUI")]
    [SerializeField] private PhaseTimerView _battleTimerView;
    [SerializeField] private PhaseTimerView _skillSelectTimerView;
    [SerializeField] private PhaseTimerView _gameOverTimerView;

    #endregion

    #region フィールド変数

    private SequenceStateMachine _stateMachine;
    private SequenceStateContext _context;

    private PhaseTimerPresenter _battleTimerPresenter;
    private PhaseTimerPresenter _skillSelectTimerPresenter;
    private PhaseTimerPresenter _gameOverTimerPresenter;

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
        _context?.SkillSelectTimer?.Dispose();
        _context?.GameOverTimer?.Dispose();

        _battleTimerPresenter?.Dispose();
        _skillSelectTimerPresenter?.Dispose();
        _gameOverTimerPresenter?.Dispose();
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
