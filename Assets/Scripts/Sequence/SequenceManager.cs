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

    public void ApplyBGM(SequenceStateType state)
    {
        BGMType bgm = state switch
        {
            SequenceStateType.IntroMovie => _introMovieBGM,
            SequenceStateType.Tutorial => _tutorialBGM,
            SequenceStateType.MobAndSkill => _mobAndSkillBGM,
            SequenceStateType.BossIntroMovie => _bossIntroMovieBGM,
            SequenceStateType.BossBattle => _bossBattleBGM,
            SequenceStateType.EndingMovie => _endingMovieBGM,
            SequenceStateType.Result => _resultBGM,
            SequenceStateType.GameOver => _gameOverBGM,
            _ => BGMType.Silence
        };
        switch (bgm)
        {
            case BGMType.OutGameMobBattle:
                Sound.PlayBGM(SoundCueNames.BGM.OutGameMobBattle, CueSheetType.BGM);
                break;
            case BGMType.BossBattle:
                Sound.PlayBGM(SoundCueNames.BGM.BossBattle, CueSheetType.BGM);
                break;
            default:
                Sound.StopBGM();
                break;
        }
    }

    public void ApplyLighting(SequenceStateType state)
    {
        SequenceLighting settings = state switch
        {
            SequenceStateType.IntroMovie => _mobLighting,
            SequenceStateType.Result => _resultLighting,
            _ => null
        };
        settings?.Apply();
    }

    /// <summary>TimelineのSignal Receiverから、ムービー中の任意のタイミングで呼び出す。</summary>
    public void ApplyBossLighting()
    {
        _bossLighting?.Apply();
    }

    /// <summary>ボス戦開始時にプレイヤーの位置をリセットする</summary>
    public void SetPlayerPosition()
    {
        if (_playerStartPosition == null)
        {
            Debug.LogError("ボス戦開始時のプレイヤー位置が未設定です");
            return;
        }

        _context?.Player?.SetPositionAndRotation(_playerStartPosition.position, _playerStartPosition.rotation);
    }

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

    public enum BGMType
    {
        [InspectorName("無音")] Silence = 0,
        [InspectorName("共通BGM（タイトル・モブ戦）")] OutGameMobBattle = 1,
        [InspectorName("ボス戦BGM")] BossBattle = 2
    }

    [Header("シークエンス別BGM（各シークエンス開始時に適用）")]
    [SerializeField, InspectorName("導入ムービー")] private BGMType _introMovieBGM = BGMType.OutGameMobBattle;
    [SerializeField, InspectorName("チュートリアル")] private BGMType _tutorialBGM = BGMType.OutGameMobBattle;
    [SerializeField, InspectorName("モブ戦・スキル選択")] private BGMType _mobAndSkillBGM = BGMType.OutGameMobBattle;
    [SerializeField, InspectorName("ボス登場ムービー")] private BGMType _bossIntroMovieBGM = BGMType.Silence;
    [SerializeField, InspectorName("ボス戦")] private BGMType _bossBattleBGM = BGMType.BossBattle;
    [SerializeField, InspectorName("エンディングムービー")] private BGMType _endingMovieBGM = BGMType.Silence;
    [SerializeField, InspectorName("リザルト")] private BGMType _resultBGM = BGMType.Silence;
    [SerializeField, InspectorName("ゲームオーバー")] private BGMType _gameOverBGM = BGMType.Silence;

    [Header("Sequence設定")]
    [SerializeField, Tooltip("IntroMovie開始時に適用するモブ戦用Lighting")]
    private SequenceLighting _mobLighting = new();
    [SerializeField, Tooltip("Signal ReceiverからApplyBossLightingを呼んだときに適用")]
    private SequenceLighting _bossLighting = new();
    [SerializeField, Tooltip("Result開始時に適用するLighting")]
    private SequenceLighting _resultLighting = new();

    [SerializeField, Tooltip("ボス戦でプレイヤーがスタートする座標のTransform")] private Transform _playerStartPosition;

    [Serializable]
    private sealed class SequenceLighting
    {
        [SerializeField, Tooltip("直接参照するLightingデータ。未指定なら現在の設定を維持")]
        private EnvironmentLightingData _data;
        [SerializeField, Tooltip("Sun Sourceを変更する場合に有効化。NoneならUnityの自動選択")]
        private bool _overrideSun;
        [SerializeField] private Light _sunSource;

        public void Apply()
        {
            if (_overrideSun)
                RenderSettings.sun = _sunSource;
            if (_data != null)
                _data.Apply();
        }
    }

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
