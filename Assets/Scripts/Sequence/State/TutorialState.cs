using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 操作説明、ドロップ説明、スキル獲得を順に体験するチュートリアルシークエンス。
/// </summary>
[Serializable]
public sealed class TutorialState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.Tutorial;

    public void OnEnter(SequenceStateContext context)
    {
        _context = context;
        _phase = Phase.Battle;
        _waveController = null;
        _defeatGuideShown = false;
        _waveClearDetected = false;
        _skillSelected = false;
        _transitionRequested = false;
        _hasSuspendedRealtimeGuide = false;
        _skillSelectPending = false;
        ResetOperationProgress();

        context.InputHandler?.EnableInput(false);
        context.EnemyManager.OnEnemyDefeated += HandleEnemyDefeated;
        context.InputHandler.OnDodge += HandleDodge;
        context.Player.OnAttackHit += HandleAttackHit;
        context.Player.OnModeChanged += HandleModeChanged;
        context.InputHandler.SetModeChangeEnabled(false);
        context.InputHandler.SetLockOnEnabled(false);

        if (ServiceLocator.TryGet(out CameraManager cameraManager))
        {
            _cameraManager = cameraManager;
            _cameraManager.OnLockOnTargetChanged += HandleLockOnTargetChanged;
        }

        if (_panelView != null)
            _panelView.OnNextRequested += HandleNextRequested;

        StartTutorialWave();
        StartRealtimeGuide(TutorialTrigger.BattleStarted);
    }

    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        if (_transitionRequested)
            return _nextSequence;

        if (_phase != Phase.Battle || _panelIsOpen)
            return null;

        TickRealtimeGuide(deltaTime);

        _waveController?.Tick();

        bool waveComplete = _waveClearDetected ||
            (_waveController != null && _waveController.IsComplete);

        if (waveComplete)
        {
            _waveClearDetected = false;
            _phase = Phase.WaitingForSkillGuide;
            ShowPages(TutorialTrigger.WaveCleared);
        }

        return null;
    }

    public void OnExit(SequenceStateContext context)
    {
        context.EnemyManager.OnEnemyDefeated -= HandleEnemyDefeated;
        context.InputHandler.OnDodge -= HandleDodge;
        context.Player.OnAttackHit -= HandleAttackHit;
        context.Player.OnModeChanged -= HandleModeChanged;
        context.InputHandler.SetModeChangeEnabled(true);
        context.InputHandler.SetLockOnEnabled(true);

        if (_cameraManager != null)
            _cameraManager.OnLockOnTargetChanged -= HandleLockOnTargetChanged;

        if (_panelView != null)
        {
            _panelView.OnNextRequested -= HandleNextRequested;
            _panelView.Hide();
        }

        if (_skillSelectView != null)
            _skillSelectView.OnSkillSelected -= HandleSkillSelected;

        _skillSelectPresenter?.Dispose();
        _skillSelectPresenter = null;
        ReleasePause();

        _pagesToShow.Clear();
        _waveController = null;
        _cameraManager = null;
        _context = null;

        context.InputHandler?.EnableInput(false);
        ShowCursor();
    }

    [Header("チュートリアルUI")]
    [SerializeField] private TutorialPanelView _panelView;
    [SerializeField] private List<TutorialPage> _pages = new()
    {
        new TutorialPage(
            TutorialTrigger.BattleStarted,
            "基本操作・闘神モード",
            "移動：左スティック / WASD\n攻撃：RB / 左クリック\n回避：A / Space\n\n闘神モードは重い一撃で敵の鎧を崩すことに優れています。",
            8f),
        new TutorialPage(
            TutorialTrigger.ModeChange,
            "モードチェンジ・雷神モード",
            "モードチェンジ：Y / E\n\n雷神モードは素早い連続攻撃が得意です。雷神ゲージを消費するため、残量に注意しましょう。",
            8f),
        new TutorialPage(
            TutorialTrigger.LockOn,
            "ロックオン",
            "ロックオン：右スティック押し込み / Shift\n対象切替：右スティック / 矢印キー\n\n敵を注視しながら移動・攻撃できます。",
            8f),
        new TutorialPage(
            TutorialTrigger.FirstEnemyDefeated,
            "ドロップアイテム",
            "敵を倒すと経験値が手に入ります。回復アイテムが落ちた場合は、近づいて取得すると体力を回復できます。"),
        new TutorialPage(
            TutorialTrigger.WaveCleared,
            "スキル獲得",
            "ウェーブをクリアするとスキルを獲得できます。候補の中から、今後の戦いに役立つスキルを1つ選びましょう。"),
    };

    [Header("チュートリアル戦闘")]
    [SerializeField] private SpawnPointSelector _spawnPointSelector;
    [SerializeField, Tooltip("先頭のWaveをチュートリアルで使用します")]
    private WaveSequenceData _waveSequenceData;

    [Header("操作課題の達成条件")]
    [SerializeField, Min(0.1f)] private float _moveDuration = 2f;
    [SerializeField, Min(1)] private int _dodgeRequiredCount = 1;
    [SerializeField, Min(1)] private int _warriorNormalAttackRequiredCount = 1;
    [SerializeField, Min(1)] private int _warriorChargeAttackRequiredCount = 1;
    [SerializeField, Min(1)] private int _thunderAttackRequiredCount = 1;
    [SerializeField, Min(1)] private int _lockOnRequiredCount = 1;
    [SerializeField, Min(1)] private int _lockOnChangeRequiredCount = 1;

    [Header("スキル獲得")]
    [SerializeField] private SkillSelectView _skillSelectView;
    [SerializeField, Min(1)] private int _skillSelectCount = 3;

    [Header("シークエンス設定")]
    [SerializeField] private SequenceStateType _nextSequence = SequenceStateType.MobAndSkill;

    [Header("パネル表示中に停止する対象")]
    [SerializeField] private HitStopTargetGroup _pauseTargetGroup = HitStopTargetGroup.All;

    private enum Phase
    {
        Battle,
        WaitingForSkillGuide,
        SkillSelect,
    }

    private readonly Queue<TutorialPage> _pagesToShow = new();
    private SequenceStateContext _context;
    private WaveController _waveController;
    private SkillSelectPresenter _skillSelectPresenter;
    private CameraManager _cameraManager;
    private IDisposable _pauseHandle;
    private Phase _phase;
    private TutorialTrigger _activeTrigger;
    private bool _panelIsOpen;
    private bool _defeatGuideShown;
    private bool _waveClearDetected;
    private bool _skillSelected;
    private bool _transitionRequested;
    private bool _realtimeGuideActive;
    private float _realtimeGuideRemaining;
    private OperationStep _operationStep;
    private float _moveElapsed;
    private int _dodgeCount;
    private int _warriorNormalAttackCount;
    private int _warriorChargeAttackCount;
    private int _thunderAttackCount;
    private int _lockOnCount;
    private int _lockOnChangeCount;
    private ILockOnTarget _lastLockOnTarget;
    private bool _hasSuspendedRealtimeGuide;
    private TutorialTrigger _suspendedRealtimeTrigger;
    private bool _skillSelectPending;

    private enum OperationStep
    {
        Move,
        Dodge,
        WarriorNormalAttack,
        WarriorChargeAttack,
        ThunderModeChange,
        ThunderAttack,
        LockOn,
        LockOnChange,
    }

    private void HandleEnemyDefeated()
    {
        _waveController?.OnEnemyDefeated();

        if (!_defeatGuideShown)
        {
            _defeatGuideShown = true;
            ShowPages(TutorialTrigger.FirstEnemyDefeated);
        }

        if (_waveController != null && _waveController.IsComplete)
            _waveClearDetected = true;
    }

    private void ShowPages(TutorialTrigger trigger)
    {
        SuspendRealtimeGuide();
        _pagesToShow.Clear();
        _activeTrigger = trigger;

        if (_pages != null)
        {
            foreach (var page in _pages)
            {
                if (page != null && page.Trigger == trigger)
                    _pagesToShow.Enqueue(page);
            }
        }

        if (_panelView == null || _pagesToShow.Count == 0)
        {
            CompleteGuide(trigger);
            return;
        }

        _panelIsOpen = true;
        BeginPause();
        _panelView.Show(_pagesToShow.Dequeue(), true);
    }

    private void HandleNextRequested()
    {
        if (!_panelIsOpen)
            return;

        if (_pagesToShow.Count > 0)
        {
            _panelView.Show(_pagesToShow.Dequeue(), true);
            return;
        }

        _panelView.Hide();
        _panelIsOpen = false;
        ReleasePause();
        CompleteGuide(_activeTrigger);
    }

    private void CompleteGuide(TutorialTrigger trigger)
    {
        switch (trigger)
        {
            case TutorialTrigger.FirstEnemyDefeated:
                ResumeSuspendedRealtimeGuide();
                break;
            case TutorialTrigger.WaveCleared:
                if (_hasSuspendedRealtimeGuide)
                {
                    _skillSelectPending = true;
                    StartTutorialWave();
                    ResumeSuspendedRealtimeGuide();
                }
                else
                {
                    StartSkillSelect();
                }
                break;
        }
    }

    private void StartTutorialWave()
    {
        if (_waveSequenceData == null || _waveSequenceData.Waves == null ||
            _waveSequenceData.Waves.Count == 0 || _spawnPointSelector == null)
        {
            Debug.LogError("[TutorialState] WaveSequenceData または SpawnPointSelector が未設定です。");
            _transitionRequested = true;
            return;
        }

        _waveController = new WaveController(_context.EnemyManager, _spawnPointSelector);
        if (!_waveController.StartWave(_waveSequenceData.Waves[0]))
        {
            Debug.LogError("[TutorialState] チュートリアルWaveの開始に失敗しました。");
            _transitionRequested = true;
            return;
        }

        ResumeBattle();
    }

    private void StartRealtimeGuide(TutorialTrigger trigger)
    {
        _pagesToShow.Clear();
        _activeTrigger = trigger;

        if (_pages != null)
        {
            foreach (var page in _pages)
            {
                if (page != null && page.Trigger == trigger)
                    _pagesToShow.Enqueue(page);
            }
        }

        ShowNextRealtimePage();
    }

    private void ShowNextRealtimePage()
    {
        if (_panelView != null && _pagesToShow.Count > 0)
        {
            var page = _pagesToShow.Dequeue();
            _realtimeGuideActive = true;
            _realtimeGuideRemaining = _activeTrigger == TutorialTrigger.LockOn
                ? page.Duration
                : float.PositiveInfinity;
            _panelView.Show(page, false);
            UpdateOperationProgress();
            return;
        }

        AdvanceRealtimeGuide();
    }

    private void TickRealtimeGuide(float deltaTime)
    {
        if (!_realtimeGuideActive)
            return;

        if (_activeTrigger == TutorialTrigger.BattleStarted)
        {
            if (_operationStep == OperationStep.Move &&
                _context.InputHandler.MoveInput.sqrMagnitude > 0.01f)
            {
                _moveElapsed = Mathf.Min(_moveDuration, _moveElapsed + deltaTime);
                if (_moveElapsed >= _moveDuration)
                    _operationStep = OperationStep.Dodge;

                UpdateOperationProgress();
            }
            return;
        }

        if (_activeTrigger == TutorialTrigger.ModeChange ||
            _activeTrigger == TutorialTrigger.LockOn)
            return;

        _realtimeGuideRemaining -= deltaTime;
        if (_realtimeGuideRemaining > 0f)
            return;

        _realtimeGuideActive = false;
        ShowNextRealtimePage();
    }

    private void AdvanceRealtimeGuide()
    {
        switch (_activeTrigger)
        {
            case TutorialTrigger.BattleStarted:
                _context.InputHandler.SetModeChangeEnabled(true);
                _operationStep = OperationStep.ThunderModeChange;
                StartRealtimeGuide(TutorialTrigger.ModeChange);
                break;
            case TutorialTrigger.ModeChange:
                _context.InputHandler.SetLockOnEnabled(true);
                _operationStep = OperationStep.LockOn;
                StartRealtimeGuide(TutorialTrigger.LockOn);
                break;
            default:
                EndRealtimeGuide();
                break;
        }
    }

    private void ResetOperationProgress()
    {
        _operationStep = OperationStep.Move;
        _moveElapsed = 0f;
        _dodgeCount = 0;
        _warriorNormalAttackCount = 0;
        _warriorChargeAttackCount = 0;
        _thunderAttackCount = 0;
        _lockOnCount = 0;
        _lockOnChangeCount = 0;
        _lastLockOnTarget = null;
    }

    private void HandleDodge()
    {
        if (!_realtimeGuideActive ||
            _activeTrigger != TutorialTrigger.BattleStarted ||
            _operationStep != OperationStep.Dodge)
            return;

        _dodgeCount++;
        if (_dodgeCount >= _dodgeRequiredCount)
            _operationStep = OperationStep.WarriorNormalAttack;

        UpdateOperationProgress();
    }

    private void HandleAttackHit(PlayerMode mode, ChargeLevel chargeLevel)
    {
        if (!_realtimeGuideActive)
            return;

        if (_activeTrigger == TutorialTrigger.BattleStarted && mode == PlayerMode.Warrior)
        {
            if (_operationStep == OperationStep.WarriorNormalAttack && chargeLevel == ChargeLevel.None)
            {
                _warriorNormalAttackCount++;
                if (_warriorNormalAttackCount >= _warriorNormalAttackRequiredCount)
                    _operationStep = OperationStep.WarriorChargeAttack;
            }
            else if (_operationStep == OperationStep.WarriorChargeAttack && chargeLevel > ChargeLevel.None)
            {
                _warriorChargeAttackCount++;
                if (_warriorChargeAttackCount >= _warriorChargeAttackRequiredCount)
                {
                    _realtimeGuideActive = false;
                    AdvanceRealtimeGuide();
                    return;
                }
            }
        }
        else if (_activeTrigger == TutorialTrigger.ModeChange &&
                 _operationStep == OperationStep.ThunderAttack &&
                 mode == PlayerMode.Thunder)
        {
            _thunderAttackCount++;
            if (_thunderAttackCount >= _thunderAttackRequiredCount)
            {
                _realtimeGuideActive = false;
                AdvanceRealtimeGuide();
                return;
            }
        }

        UpdateOperationProgress();
    }

    private void HandleModeChanged(PlayerMode mode)
    {
        if (!_realtimeGuideActive ||
            _activeTrigger != TutorialTrigger.ModeChange ||
            _operationStep != OperationStep.ThunderModeChange ||
            mode != PlayerMode.Thunder)
            return;

        _operationStep = OperationStep.ThunderAttack;
        UpdateOperationProgress();
    }

    private void HandleLockOnTargetChanged(ILockOnTarget target)
    {
        if (!_realtimeGuideActive || _activeTrigger != TutorialTrigger.LockOn)
        {
            _lastLockOnTarget = target;
            return;
        }

        if (_operationStep == OperationStep.LockOn &&
            target != null && _lastLockOnTarget == null)
        {
            _lockOnCount++;
            if (_lockOnCount >= _lockOnRequiredCount)
                _operationStep = OperationStep.LockOnChange;
        }
        else if (_operationStep == OperationStep.LockOnChange &&
                 target != null && _lastLockOnTarget != null &&
                 target != _lastLockOnTarget)
        {
            _lockOnChangeCount++;
            if (_lockOnChangeCount >= _lockOnChangeRequiredCount)
            {
                _lastLockOnTarget = target;
                _realtimeGuideActive = false;
                AdvanceRealtimeGuide();
                return;
            }
        }

        _lastLockOnTarget = target;
        UpdateOperationProgress();
    }

    private void UpdateOperationProgress()
    {
        if (_panelView == null)
            return;

        string progress = _operationStep switch
        {
            OperationStep.Move => $"移動する  {_moveElapsed:0.0} / {_moveDuration:0.0} 秒",
            OperationStep.Dodge => $"回避する  {_dodgeCount} / {_dodgeRequiredCount} 回",
            OperationStep.WarriorNormalAttack =>
                $"闘神の通常攻撃を当てる  {_warriorNormalAttackCount} / {_warriorNormalAttackRequiredCount} 回",
            OperationStep.WarriorChargeAttack =>
                $"闘神のチャージ攻撃を当てる  {_warriorChargeAttackCount} / {_warriorChargeAttackRequiredCount} 回",
            OperationStep.ThunderModeChange => "雷神モードへチェンジする",
            OperationStep.ThunderAttack =>
                $"雷神の攻撃を当てる  {_thunderAttackCount} / {_thunderAttackRequiredCount} 回",
            OperationStep.LockOn =>
                $"敵をロックオンする  {_lockOnCount} / {_lockOnRequiredCount} 回",
            OperationStep.LockOnChange =>
                $"ロックオン対象を切り替える  {_lockOnChangeCount} / {_lockOnChangeRequiredCount} 回",
            _ => string.Empty,
        };

        _panelView.SetProgress(progress);
    }

    private void EndRealtimeGuide()
    {
        _realtimeGuideActive = false;
        _realtimeGuideRemaining = 0f;
        _pagesToShow.Clear();
        _panelView?.Hide();

        if (_skillSelectPending)
        {
            _skillSelectPending = false;
            _context.EnemyManager.ClearAllMobEnemies();
            StartSkillSelect();
        }
    }

    private void SuspendRealtimeGuide()
    {
        if (!_realtimeGuideActive)
            return;

        _hasSuspendedRealtimeGuide = true;
        _suspendedRealtimeTrigger = _activeTrigger;
        _realtimeGuideActive = false;
        _pagesToShow.Clear();
        _panelView?.Hide();
    }

    private void ResumeSuspendedRealtimeGuide()
    {
        if (!_hasSuspendedRealtimeGuide)
        {
            ResumeBattle();
            return;
        }

        var trigger = _suspendedRealtimeTrigger;
        _hasSuspendedRealtimeGuide = false;
        ResumeBattle();
        StartRealtimeGuide(trigger);
    }

    private void ResumeBattle()
    {
        _phase = Phase.Battle;
        _context.InputHandler?.EnableInput(true);
        HideCursor();
    }

    private void StartSkillSelect()
    {
        _phase = Phase.SkillSelect;
        _context.InputHandler?.EnableInput(false);
        ShowCursor();
        BeginPause();

        if (_skillSelectView == null)
        {
            Debug.LogWarning("[TutorialState] SkillSelectView が未設定のため、スキル選択をスキップします。");
            FinishTutorial();
            return;
        }

        _skillSelectPresenter = new SkillSelectPresenter(
            _context.SkillManager,
            _skillSelectView,
            _context.Player);

        if (!_skillSelectPresenter.Open(_skillSelectCount))
        {
            FinishTutorial();
            return;
        }

        _skillSelectView.OnSkillSelected += HandleSkillSelected;
    }

    private void HandleSkillSelected(int _)
    {
        if (_skillSelected)
            return;

        _skillSelected = true;
        FinishTutorial();
    }

    private void FinishTutorial()
    {
        if (_skillSelectView != null)
            _skillSelectView.OnSkillSelected -= HandleSkillSelected;

        _skillSelectPresenter?.Dispose();
        _skillSelectPresenter = null;
        ReleasePause();
        _transitionRequested = true;
    }

    private void BeginPause()
    {
        ReleasePause();
        _context?.InputHandler?.EnableInput(false);
        ShowCursor();

        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
            _pauseHandle = hitStopManager.BeginManualStop(_pauseTargetGroup);
    }

    private void ReleasePause()
    {
        _pauseHandle?.Dispose();
        _pauseHandle = null;
    }

    private static void HideCursor() => Cursor.visible = false;

    private static void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
