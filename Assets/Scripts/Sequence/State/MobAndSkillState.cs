using System;
using UnityEngine;

/// <summary>
/// モブ戦とスキル選択を交互に繰り返すState。
/// 内部でサブフェーズ（戦闘中 / スキル選択中）を持つ。
/// 3分タイマーが切れたら BossIntroMovie へ遷移する。
/// </summary>
[Serializable]
public class MobAndSkillState : ISequenceState
{
    #region パブリック

    public SequenceStateType StateType => SequenceStateType.MobAndSkill;

    public void OnEnter(SequenceStateContext context)
    {
        _currentWaveIndex = 0;
        _waveCleared = false;
        _timeUpFlag = false;
        _isSkillSelected = false;
        _skillSelectTimeUp = false;
        _waveController = null;

        _mobBattleTimer = new CountDownTimer();
        _skillSelectTimer = new CountDownTimer();

        if (_mobBattleTimerView != null)
            _mobBattleTimerPresenter = new CountDownTimerPresenter(_mobBattleTimer, _mobBattleTimerView);

        if (_skillSelectTimerView != null)
            _skillSelectTimerPresenter = new CountDownTimerPresenter(_skillSelectTimer, _skillSelectTimerView);

        // タイマー開始
        _mobBattleTimer.OnTimeEnded += OnMobTimeUp;
        _mobBattleTimer.StartTimer(_mobBattleTimeLimit);

        // EnemyManagerのウェーブクリア検知
        context.EnemyManager.OnEnemyDefeated += OnEnemyDefeated;

        // 入力有効化
        context.InputHandler?.EnableInput(true);

        // 最初のウェーブをスポーン
        StartNextWave(context);

        _subPhase = SubPhase.Battle;
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        // 死亡はStateMachineが外部から強制遷移させるため、ここでは見ない

        // タイマー切れ → ボス登場へ
        if (context.IsTimeUp)
        {
            context.EnemyManager.ClearAllMobEnemies(); // タイマー切れと同時に敵を全て消す
            return _nextSequence;
        }

        return _subPhase switch
        {
            SubPhase.Battle => TickBattle(context),
            SubPhase.SkillSelect => TickSkillSelect(context),
            _ => null
        };
    }

    public void OnExit(SequenceStateContext context)
    {
        _mobBattleTimer.StopTimer();
        _mobBattleTimer.OnTimeEnded -= OnMobTimeUp;

        if (_skillSelectTimer != null)
        {
            _skillSelectTimer.OnTimeEnded -= OnSkillSelectTimeUp;
            _skillSelectTimer.StopTimer();
        }

        _skillSelectView.OnSkillSelected -= OnSkillSelected;

        context.EnemyManager.OnEnemyDefeated -= OnEnemyDefeated;

        _skillSelectPresenter?.Dispose();
        _skillSelectPresenter = null;

        _mobBattleTimerPresenter?.Dispose();
        _mobBattleTimerPresenter = null;

        _skillSelectTimerPresenter?.Dispose();
        _skillSelectTimerPresenter = null;

        _waveController = null;

        context.InputHandler?.EnableInput(false);
    }

    #endregion

    #region シリアライズ

    [SerializeField] private string _stateName = "MobAndSkillState";

    [Header("モブ戦")]
    [SerializeField, Tooltip("モブ戦のタイマーUI")] private CountDownTimerView _mobBattleTimerView;
    [SerializeField, Tooltip("スキル選択のタイマーUI")] private CountDownTimerView _skillSelectTimerView;
    [SerializeField, Tooltip("モブ戦の時間制限（秒）")] private float _mobBattleTimeLimit = 180f;
    [SerializeField, Tooltip("スポーンポイントのセレクター")] private SpawnPointSelector _spawnPointSelector;
    [SerializeField, Tooltip("ウェーブデータ")] private WaveSequenceData _waveSequenceData;

    [Header("スキル選択")]
    [SerializeField] private SkillSelectView _skillSelectView;
    [SerializeField] private float _skillSelectTimeLimit = 10f;
    [SerializeField] private int _skillSelectCount = 3;

    [Header("シークエンス設定")]
    [SerializeField] private SequenceStateType _nextSequence = SequenceStateType.BossIntroMovie;

    #endregion

    #region　プライベート

    private enum SubPhase { Battle, SkillSelect }

    private SubPhase _subPhase;
    private int _currentWaveIndex;
    private WaveController _waveController;
    private SkillSelectPresenter _skillSelectPresenter;

    private CountDownTimer _mobBattleTimer;
    private CountDownTimer _skillSelectTimer;

    private CountDownTimerPresenter _mobBattleTimerPresenter;
    private CountDownTimerPresenter _skillSelectTimerPresenter;

    private bool _waveCleared;
    private bool _timeUpFlag;
    private bool _isSkillSelected;
    private bool _skillSelectTimeUp;

    #endregion

    #region　イベントハンドラ

    private void OnMobTimeUp() => _timeUpFlag = true;
    private void OnSkillSelectTimeUp() => _skillSelectTimeUp = true;
    private void OnSkillSelected(int _) => _isSkillSelected = true;

    private void OnEnemyDefeated()
    {
        _waveController?.OnEnemyDefeated();

        // WaveController の IsComplete はループ内で Tick するが、
        // 最後の敵が倒れた瞬間にフラグを立てて次フレームで検知する
        if (_waveController != null && _waveController.IsComplete)
            _waveCleared = true;
    }

    #endregion

    #region Wave制御

    /// <summary>
    /// 次のウェーブを開始する。ウェーブデータがない or 全ウェーブ終了している場合は全ウェーブ終了フラグを立てる。
    /// </summary>
    /// <param name="context"></param>
    private void StartNextWave(SequenceStateContext context)
    {
        var waveSequence = _waveSequenceData;

        if (waveSequence == null || waveSequence.Waves == null
            || waveSequence.Waves.Count == 0)
        {
            // ウェーブデータがない
            return;
        }

        // あまりを利用してウェーブをループさせる。
        var waveData = waveSequence.Waves[_currentWaveIndex % waveSequence.Waves.Count];

        if (_spawnPointSelector == null)
        {
            Debug.LogError("[MobAndSkillState] SpawnPointSelectorが未設定です");
            return;
        }

        _waveController = new WaveController(context.EnemyManager, _spawnPointSelector);

        // ウェーブ開始に失敗したら、以降のウェーブも開始できないので全ウェーブ終了フラグを立てる
        if (!_waveController.StartWave(waveData))
        {
            Debug.LogError($"[MobAndSkillState] ウェーブの開始に失敗しました: WaveIndex={_currentWaveIndex}");
            return;
        }
    }

    #endregion

    #region Tick処理

    private SequenceStateType? TickBattle(SequenceStateContext context)
    {
        if (_timeUpFlag)
        {
            context.IsTimeUp = true;
            return null; // 次のTickでIsTimeUpを検知
        }

        // ウェーブを進行
        _waveController?.Tick();

        bool waveComplete = _waveController != null && _waveController.IsComplete;

        if (_waveCleared || waveComplete)
        {
            _waveCleared = false;

            _currentWaveIndex++;

            StartSkillSelect(context);
        }

        return null;
    }

    private SequenceStateType? TickSkillSelect(SequenceStateContext context)
    {
        if (_timeUpFlag)
        {
            // バトルタイマー切れ：スキル選択を強制終了してからボスへ
            ForceAutoSelect(context);
            context.IsTimeUp = true;
            return null;
        }

        if (_skillSelectTimeUp)
        {
            _skillSelectTimeUp = false;
            ForceAutoSelect(context);
        }

        if (_isSkillSelected)
        {
            _isSkillSelected = false;
            EndSkillSelect(context);
        }

        return null;
    }

    #endregion

    #region Skill選択制御

    private void StartSkillSelect(SequenceStateContext context)
    {
        _subPhase = SubPhase.SkillSelect;
        _isSkillSelected = false;
        _skillSelectTimeUp = false;

        // 世界を止める
        _mobBattleTimer.PauseTimer();
        context.InputHandler?.EnableInput(false);

        // スキル選択タイマー開始
        if (_skillSelectTimer != null)
        {
            _skillSelectTimer.OnTimeEnded += OnSkillSelectTimeUp;
            _skillSelectTimer.StartTimer(_skillSelectTimeLimit);
        }

        // スキル選択UIを開く
        _skillSelectPresenter?.Dispose();
        _skillSelectPresenter = new SkillSelectPresenter(
            context.SkillManager,
            _skillSelectView,
            context.Player
        );

        bool hasSkill = _skillSelectPresenter.Open(_skillSelectCount);
        if (!hasSkill)
        {
            // 候補がない場合は即戦闘復帰
            EndSkillSelect(context);
        }
        else
        {
            _skillSelectView.OnSkillSelected += OnSkillSelected;
        }
    }

    private void EndSkillSelect(SequenceStateContext context)
    {
        _skillSelectView.OnSkillSelected -= OnSkillSelected;

        if (_skillSelectTimer != null)
        {
            _skillSelectTimer.OnTimeEnded -= OnSkillSelectTimeUp;
            _skillSelectTimer.StopTimer();
        }

        _skillSelectPresenter?.Dispose();
        _skillSelectPresenter = null;

        // 世界を再開
        _mobBattleTimer.ResumeTimer();
        context.InputHandler?.EnableInput(true);

        _subPhase = SubPhase.Battle;

        // 次のWaveを開始
        StartNextWave(context);
    }

    private void ForceAutoSelect(SequenceStateContext context)
    {
        _skillSelectPresenter?.AutoSelect();
        EndSkillSelect(context);
    }

    #endregion
}
