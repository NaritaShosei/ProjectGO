using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SequenceManager : MonoBehaviour
{
    #region パブリック

    /// <summary>全シークエンスクリア（リザルトへ）</summary>
    public event Action OnAllSequencesComplete;

    /// <summary>タイトルへ戻るリクエスト（ゲームオーバー後）</summary>
    public event Action OnTitleRequested;
    public SubtitleController Subtitles { get; private set; }

    public async UniTask InitializeAsync(EnemyManager enemyManager, SkillManager skillManager, InputHandler inputHandler, IPlayer player)
    {
        if (enemyManager == null || skillManager == null || inputHandler == null || player == null)
        {
            Debug.LogError("EnemyManager、SkillManagerが未設定です");
            return;
        }
        var cancellationToken = this.GetCancellationTokenOnDestroy();
        SubtitleSettings subtitleSettings = null;
        _isLoadingSubtitleSettings = true;
        try
        {
            // 共通ローダーのキャッシュを利用し、字幕が準備できてからシークエンスを初期化する。
            subtitleSettings = await AssetsLoader.LoadAssetAsync<SubtitleSettings>(_subtitleSettingsAddress);
        }
        catch (Exception exception)
        {
            AssetsLoader.Release(_subtitleSettingsAddress);
            if (!cancellationToken.IsCancellationRequested)
                Debug.LogError($"[SequenceManager] 字幕設定を読み込めませんでした: {exception.Message}", this);
        }
        finally
        {
            _isLoadingSubtitleSettings = false;
            // ロード中のシーン破棄では完了を待ってから解放し、待機中のハンドルを無効化しない。
            if (cancellationToken.IsCancellationRequested)
                AssetsLoader.Release(_subtitleSettingsAddress);
        }
        cancellationToken.ThrowIfCancellationRequested();

        // 設定のロード失敗時もコントローラー経由でボイスの再生・停止を管理する。
        Subtitles = gameObject.AddComponent<SubtitleController>();
        Subtitles.InitializeController(subtitleSettings, player as Player, _subtitleView);

        // コンテキスト構築
        _context = new SequenceStateContext
        {
            EnemyManager = enemyManager,
            SkillManager = skillManager,
            EXPManager = ServiceLocator.TryGet(out EXPManager expManager) ? expManager : null,
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

    /// <summary>ResultSequenceから呼ばれる</summary>
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
    [SerializeField, Tooltip("AssetsLoaderで読み込む字幕設定のAddressablesアドレス")]
    private string _subtitleSettingsAddress = "SubtitleSettings";
    [SerializeField, Tooltip("事前配置した字幕用Text UIを参照するView")]
    private SubtitleView _subtitleView;
    [SerializeField, Tooltip("シークエンス内で共通して使用するMoviePlayer")] private MoviePlayer _moviePlayer;
    [SerializeField, Tooltip("最初に開始するシークエンスのタイプ")] private SequenceStateType _firstSequence = SequenceStateType.IntroMovie;
    [SerializeReference, SubclassSelector]
    private ISequenceState[] _sequences = new ISequenceState[]
    {
        new IntroMovieState(),
        new TutorialState(),
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
    private bool _isLoadingSubtitleSettings;

    #endregion

    #region Unityイベント

    private void OnValidate()
    {
        if (_sequences != null) return;

        _sequences = new ISequenceState[]
        {
            new IntroMovieState(),
            new TutorialState(),
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
        if (!_isLoadingSubtitleSettings)
            AssetsLoader.Release(_subtitleSettingsAddress);
    }

    #endregion

    #region プライベートメソッド

    private void RegisterStates()
    {
        if (_sequences == null)
        {
            Debug.LogError("[SequenceManager] Sequences are missing.", this);
            return;
        }

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
        _context.GameOverReason = GameOverReason.PlayerHealthDepleted;
        _stateMachine?.ForceTransition(SequenceStateType.GameOver);
    }

    #endregion
}
