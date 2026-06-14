using UnityEngine;

/// <summary>
/// モブ戦とスキル選択を交互に繰り返すState。
/// 内部でサブフェーズ（戦闘中 / スキル選択中）を持つ。
/// 3分タイマーが切れたら BossIntroMovie へ遷移する。
/// </summary>
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

        // タイマー開始
        context.PhaseTimer.StartTimer(context.MobBattleTimeLimit);
        context.PhaseTimer.OnTimeEnded += OnMobTimeUp;

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
            return SequenceStateType.BossIntroMovie;
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
        context.PhaseTimer.StopTimer();
        context.PhaseTimer.OnTimeEnded -= OnMobTimeUp;

        if (context.SkillSelectTimer != null)
        {
            context.SkillSelectTimer.OnTimeEnded -= OnSkillSelectTimeUp;
            context.SkillSelectTimer.StopTimer();
        }

        context.SkillSelectView.OnSkillSelected -= OnSkillSelected;

        context.EnemyManager.OnEnemyDefeated -= OnEnemyDefeated;

        _skillSelectPresenter?.Dispose();
        _skillSelectPresenter = null;

        _waveController = null;

        context.InputHandler?.EnableInput(false);
    }

    #endregion

    #region　プライベート

    private enum SubPhase { Battle, SkillSelect }

    private SubPhase _subPhase;
    private int _currentWaveIndex;
    private WaveController _waveController;
    private SkillSelectPresenter _skillSelectPresenter;

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
        var waveSequence = context.WaveSequenceData;

        if (waveSequence == null || waveSequence.Waves == null
            || waveSequence.Waves.Count == 0)
        {
            // ウェーブデータがない
            return;
        }

        // あまりを利用してウェーブをループさせる。
        var waveData = waveSequence.Waves[_currentWaveIndex % waveSequence.Waves.Count];

        if (context.SpawnPointSelector == null)
        {
            Debug.LogError("[MobAndSkillState] SpawnPointSelectorが未設定です");
            return;
        }

        _waveController = new WaveController(context.EnemyManager, context.SpawnPointSelector);

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
        context.PhaseTimer.PauseTimer();
        context.InputHandler?.EnableInput(false);

        // スキル選択タイマー開始
        if (context.SkillSelectTimer != null)
        {
            context.SkillSelectTimer.StartTimer(context.SkillSelectTimeLimit);
            context.SkillSelectTimer.OnTimeEnded += OnSkillSelectTimeUp;
        }

        // スキル選択UIを開く
        _skillSelectPresenter?.Dispose();
        _skillSelectPresenter = new SkillSelectPresenter(
            context.SkillManager,
            context.SkillSelectView,
            context.Player
        );

        bool hasSkill = _skillSelectPresenter.Open(context.SkillSelectCount);
        if (!hasSkill)
        {
            // 候補がない場合は即戦闘復帰
            EndSkillSelect(context);
        }
        else
        {
            context.SkillSelectView.OnSkillSelected += OnSkillSelected;
        }
    }

    private void EndSkillSelect(SequenceStateContext context)
    {
        context.SkillSelectView.OnSkillSelected -= OnSkillSelected;

        if (context.SkillSelectTimer != null)
        {
            context.SkillSelectTimer.OnTimeEnded -= OnSkillSelectTimeUp;
            context.SkillSelectTimer.StopTimer();
        }

        _skillSelectPresenter?.Dispose();
        _skillSelectPresenter = null;

        // 世界を再開
        context.PhaseTimer.ResumeTimer();
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
