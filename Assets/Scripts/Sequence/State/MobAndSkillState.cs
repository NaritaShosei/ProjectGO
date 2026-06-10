using UnityEngine;

/// <summary>
/// モブ戦とスキル選択を交互に繰り返すState。
/// 内部でサブフェーズ（戦闘中 / スキル選択中）を持つ。
/// 3分タイマーが切れたら BossIntroMovie へ遷移する。
/// </summary>
public class MobAndSkillState : ISequenceState
{
    /// <summary>モブ戦全体の制限時間（秒）</summary>
    private const float MobBattleDuration = 180f;

    /// <summary>スキル選択の制限時間（秒）</summary>
    private const float SkillSelectDuration = 10f;

    public SequenceStateType StateType => SequenceStateType.MobAndSkill;

      public void OnEnter(SequenceStateContext context)
    {
        _waveEnemySequenceIndex = 0;

        // 3分タイマー開始
        context.PhaseTimer.StartTimer(MobBattleDuration);
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

        switch (_subPhase)
        {
            case SubPhase.Battle:
                return TickBattle(context);

            case SubPhase.SkillSelect:
                return TickSkillSelect(context, deltaTime);
        }

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        context.PhaseTimer.StopTimer();
        context.PhaseTimer.OnTimeEnded -= OnMobTimeUp;
        context.EnemyManager.OnEnemyDefeated -= CheckWaveClear;

        _skillSelectPresenter?.Dispose();
        _skillSelectPresenter = null;

        context.InputHandler?.EnableInput(false);

        // IsTimeUpはResetTransitionFlagsで消えるため不要
    }

    // ── プライベート ────────────────────────────────
    private enum SubPhase { Battle, SkillSelect }

    private SubPhase _subPhase;
    private int _waveEnemySequenceIndex;
    private SkillSelectPresenter _skillSelectPresenter;

    private bool _waveCleared;
    private bool _timeUpFlag;
    private float _skillSelectTimer;
    private bool _isSkillSelected;

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

    private SequenceStateType? TickSkillSelect(SequenceStateContext context, float deltaTime)
    {
        if (_timeUpFlag)
        {
            // タイマー切れ中でもスキル選択は終わらせてからボスへ
            ForceSelectDefaultSkill(context);
            context.IsTimeUp = true;
            return null;
        }

        // スキル選択タイマー
        _skillSelectTimer -= deltaTime;
        if (_skillSelectTimer <= 0f)
        {
            ForceSelectDefaultSkill(context);
        }

        if (_isSkillSelected)
        {
            _isSkillSelected = false; // フラグをリセット
            context.IsSkillSelected = true; // Tick内でフラグを立てる（UIからの選択を反映）
        }

        if (context.IsSkillSelected)
        {
            EndSkillSelect(context);
        }

        return null;
    }

    private void StartSkillSelect(SequenceStateContext context)
    {
        context.IsSkillSelected = false;

        _subPhase = SubPhase.SkillSelect;
        _skillSelectTimer = SkillSelectDuration;

        // 世界を止める
        context.PhaseTimer.PauseTimer();
        context.InputHandler?.EnableInput(false);

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

    private void ForceSelectDefaultSkill(SequenceStateContext context)
    {
        // 時間切れ時は現在選択中のスキルを自動取得（UIの先頭）
        // SkillSelectPresenterは候補リストの先頭を自動選択する仕様
        _skillSelectPresenter.AutoSelect();
        context.IsSkillSelected = true;
    }

    private void OnSkillSelected(int _)
    {
        // SkillSelectPresenter経由でスキルが選ばれた
        _isSkillSelected = true;
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
