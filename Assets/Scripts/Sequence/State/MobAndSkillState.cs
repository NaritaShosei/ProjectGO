using UnityEngine;

/// <summary>
/// モブ戦とスキル選択を交互に繰り返すState。
/// 内部でサブフェーズ（戦闘中 / スキル選択中）を持つ。
/// 3分タイマーが切れたら BossIntroMovie へ遷移する。
/// </summary>
public class MobAndSkillState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.MobAndSkill;

    public void OnEnter(SequenceStateContext context)
    {
        _waveEnemySequenceIndex = 0;
        _waveCleared = false;
        _timeUpFlag = false;
        _isSkillSelected = false;

        // タイマー開始
        context.PhaseTimer.StartTimer(context.MobBattleTimeLimit);
        context.PhaseTimer.OnTimeEnded += OnMobTimeUp;

        // EnemyManagerのウェーブクリア検知
        context.EnemyManager.OnEnemyDefeated += CheckWaveClear;

        // 入力有効化
        context.InputHandler?.EnableInput(true);

        // 最初のウェーブをスポーン
        SpawnWave(context);

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

        context.EnemyManager.OnEnemyDefeated -= CheckWaveClear;

        _skillSelectPresenter?.Dispose();
        _skillSelectPresenter = null;

        context.InputHandler?.EnableInput(false);
    }

    // ── プライベート ────────────────────────────────
    private enum SubPhase { Battle, SkillSelect }

    private SubPhase _subPhase;
    private int _waveEnemySequenceIndex;
    private SkillSelectPresenter _skillSelectPresenter;

    private bool _waveCleared;
    private bool _timeUpFlag;
    private bool _isSkillSelected;
    private bool _skillSelectTimeUp;

    private void OnMobTimeUp()
    {
        _timeUpFlag = true;
    }

    private void CheckWaveClear()
    {
        // 残敵数が0になったらウェーブクリア
        // ※EnemyManager.GetEnemyCount()は次フレームで0になる場合があるため
        //   フラグ経由で次のTickで判定する
        _waveCleared = true;
    }

    private SequenceStateType? TickBattle(SequenceStateContext context)
    {
        if (_timeUpFlag)
        {
            context.IsTimeUp = true;
            return null; // 次のTickでIsTimeUpを検知
        }

        if (_waveCleared && context.EnemyManager.GetEnemyCount() == 0)
        {
            _waveCleared = false;
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

    private void StartSkillSelect(SequenceStateContext context)
    {
        _subPhase = SubPhase.SkillSelect;
        _isSkillSelected = false;
        _skillSelectTimeUp = false;

        // 世界を止める
        context.PhaseTimer.PauseTimer();
        context.InputHandler?.EnableInput(false);

        // スキル選択タイマー開始
        context.SkillSelectTimer.StartTimer(context.SkillSelectTimeLimit);
        if (context.SkillSelectTimer != null)
            context.SkillSelectTimer.OnTimeEnded += OnSkillSelectTimeUp;

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

        // 次のウェーブをスポーン
        _waveEnemySequenceIndex++;
        SpawnWave(context);
    }

    private void ForceAutoSelect(SequenceStateContext context)
    {
        _skillSelectPresenter?.AutoSelect();
        EndSkillSelect(context);
    }

    private void OnSkillSelected(int _)
    {
        // SkillSelectPresenter経由でスキルが選ばれた
        _isSkillSelected = true;
    }

    private void OnSkillSelectTimeUp()
    {
        // スキル選択タイマー切れ
        _skillSelectTimeUp = true;
    }

    private void SpawnWave(SequenceStateContext context)
    {
        if (context.SpawnDataRepository == null) return;
        var datas = context.SpawnDataRepository.SpawnDatas;
        if (datas == null || datas.Length == 0) return;

        int index = _waveEnemySequenceIndex % datas.Length;
        var strategy = datas[index].CreateStrategy(context.EnemyManager);
        strategy.Spawn();
    }
}
