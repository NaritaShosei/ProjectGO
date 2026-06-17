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
        // コンテキスト構築
        _context = new SequenceStateContext
        {
            EnemyManager = enemyManager,
            SkillManager = skillManager,
            InputHandler = inputHandler,
            Player = player,
            SequenceManager = this,
            MoviePlayer = _moviePlayer,
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
        _stateMachine?.Start(_firstSequence);
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

    [Header("Sequence設定")]
    [SerializeField, Tooltip("シークエンス内で共通して使用するMoviePlayer")] private MoviePlayer _moviePlayer;
    [SerializeReference, SubclassSelector,Tooltip("最初に開始するシークエンスのタイプ")] private SequenceStateType _firstSequence = SequenceStateType.IntroMovie;
    [SerializeReference, SubclassSelector]
    private ISequenceState[] _sequences = new ISequenceState[]
    {
        new IntroMovieState(),
        new MobAndSkillState(),
        new BossIntroMovieState(),
        new BossBattleState(),
        new EndingMovieState(),
        new ResultState(),
        new GameOverState(),
    };

    #endregion

    #region フィールド変数

    private SequenceStateMachine _stateMachine;
    private SequenceStateContext _context;

    #endregion

    #region Unityイベント

    private void OnValidate()
    {
        if (_sequences != null) return;

        _sequences = new ISequenceState[]
        {
            new IntroMovieState(),
            new MobAndSkillState(),
            new BossIntroMovieState(),
            new BossBattleState(),
            new EndingMovieState(),
            new ResultState(),
            new GameOverState(),
        };
    }

    private void Update()
    {
        _stateMachine?.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (_context?.Player != null)
            _context.Player.OnDead -= HandlePlayerDead;
    }

    #endregion

    #region プライベートメソッド

    private void RegisterStates()
    {
        foreach (var state in _sequences)
        {
            if (state == null)
            {
                Debug.LogWarning("Nullなシークエンスが登録されています");
                continue;
            }
            _stateMachine.RegisterState(state);
        }
    }

    private void HandlePlayerDead()
    {
        // プレイヤー死亡 → ゲームオーバーへ強制遷移
        _context.IsPlayerDead = true;
        _stateMachine?.ForceTransition(SequenceStateType.GameOver);
    }

    #endregion
}
