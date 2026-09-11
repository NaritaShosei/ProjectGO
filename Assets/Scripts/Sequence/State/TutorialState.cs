using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>最初のウェーブで基本操作からスキル獲得までを案内する。</summary>
[Serializable]
public sealed class TutorialState : ISequenceState
{
    public SequenceStateType StateType => SequenceStateType.Tutorial;

    /// <summary>チュートリアル用ウェーブと入力監視を開始する。</summary>
    public void OnEnter(SequenceStateContext context)
    {
        _context = context;
        _phase = Phase.Battle;
        _step = TutorialStep.BasicOperations;
        _waveController = null;
        _panelIsOpen = false;
        _waveClearDetected = false;
        _defeatGuideShown = false;
        _defeatGuidePending = false;
        _skillSelected = false;
        _transitionRequested = false;
        ResetBasicOperationProgress();

        context.InputHandler.EnableInput(false);
        context.InputHandler.SetModeChangeEnabled(false);
        context.InputHandler.SetLockOnEnabled(true);
        context.Player.SetTutorialModeChangeEnabled(false);
        context.InputHandler.OnDodge += HandleDodge;
        context.Player.OnAttackHit += HandleAttackHit;
        context.Player.OnArmorBroken += HandleArmorBroken;
        context.Player.OnModeChanged += HandleModeChanged;
        context.Player.OnModeChangeCompleted += HandleModeChangeCompleted;
        context.EnemyManager.OnEnemyDefeated += HandleEnemyDefeated;

        if (ServiceLocator.TryGet(out CameraManager cameraManager))
        {
            _cameraManager = cameraManager;
            _cameraManager.OnLockOnTargetChanged += HandleLockOnTargetChanged;
        }

        if (_panelView != null)
            _panelView.OnNextRequested += HandleNextRequested;

        StartTutorialWave();
        ShowRealtimePage(TutorialTrigger.BattleStarted);
    }

    /// <summary>戦闘、基本操作チェック、ウェーブ終了を更新する。</summary>
    public SequenceStateType? Tick(SequenceStateContext context, float deltaTime)
    {
        if (_transitionRequested)
            return _nextSequence;

        if (_phase != Phase.Battle || _panelIsOpen || _step == TutorialStep.WaitingModeChange)
            return null;

        if (_step == TutorialStep.BasicOperations)
            TickBasicOperations(deltaTime);

        _waveController?.Tick();
        bool waveComplete = _waveClearDetected || (_waveController != null && _waveController.IsComplete);
        if (waveComplete && _defeatGuideShown && _step == TutorialStep.ThunderCombat)
        {
            _waveClearDetected = false;
            _phase = Phase.WaitingForSkillGuide;
            ShowModalPages(TutorialTrigger.WaveCleared);
        }

        return null;
    }

    /// <summary>購読と時間停止を解除し、通常操作へ戻す。</summary>
    public void OnExit(SequenceStateContext context)
    {
        context.InputHandler.OnDodge -= HandleDodge;
        context.Player.OnAttackHit -= HandleAttackHit;
        context.Player.OnArmorBroken -= HandleArmorBroken;
        context.Player.OnModeChanged -= HandleModeChanged;
        context.Player.OnModeChangeCompleted -= HandleModeChangeCompleted;
        context.EnemyManager.OnEnemyDefeated -= HandleEnemyDefeated;
        context.InputHandler.SetModeChangeEnabled(true);
        context.InputHandler.SetLockOnEnabled(true);
        context.Player.SetTutorialModeChangeEnabled(false);

        if (_cameraManager != null)
            _cameraManager.OnLockOnTargetChanged -= HandleLockOnTargetChanged;

        if (_panelView != null)
        {
            _panelView.OnNextRequested -= HandleNextRequested;
            _panelView.Hide();
        }

        _checklistView?.Hide();

        if (_skillSelectView != null)
            _skillSelectView.OnSkillSelected -= HandleSkillSelected;

        _skillSelectPresenter?.Dispose();
        _skillSelectPresenter = null;
        ReleasePause();
        _pagesToShow.Clear();
        _waveController = null;
        _cameraManager = null;
        _context = null;
        context.InputHandler.EnableInput(false);
        ShowCursor();
    }

    [Header("チュートリアルUI")]
    [SerializeField] private TutorialChecklistView _checklistView;
    [SerializeField] private TutorialPanelView _panelView;
    [SerializeField] private List<TutorialPage> _pages = new()
    {
        new TutorialPage(TutorialTrigger.BattleStarted, "基本操作", "各操作を試しましょう。"),
        new TutorialPage(TutorialTrigger.ModeChange, "闘神モード", "RT長押しで強力な広範囲チャージ攻撃ができます。\n鎧をまとった敵には闘神モードが有効です。"),
        new TutorialPage(TutorialTrigger.LockOn, "モードチェンジ", "敵の鎧を破壊しました。\nLBで雷神モードへ切り替えてください。"),
        new TutorialPage(TutorialTrigger.FirstEnemyDefeated, "レベルアップ", "敵を倒すと経験値を獲得できます。\n経験値が一定量たまると、攻撃力・HP・雷神ゲージ・クリティカル率のいずれかが上昇します。"),
        new TutorialPage(TutorialTrigger.WaveCleared, "トールの加護", "ウェーブをクリアすると3つのスキルが提示されます。\n3つの中から1つを選んで獲得できます。"),
        new TutorialPage(TutorialTrigger.ThunderModeChanged, "雷神モード", "RT連打による連撃を得意とする高速戦闘スタイルです。\n雷神ゲージを消費しますが、鎧が剥がれた敵に強力です。\n攻撃に合わせて回避するとジャスト回避が発生します。"),
    };

    [Header("チュートリアル戦闘")]
    [SerializeField] private SpawnPointSelector _spawnPointSelector;
    [SerializeField, Tooltip("先頭のWaveをチュートリアルで使用します")] private WaveSequenceData _waveSequenceData;

    [Header("基本操作の達成条件")]
    [SerializeField, Min(0.1f)] private float _moveDuration = 1f;
    [SerializeField, Min(0.1f)] private float _cameraMoveDuration = 0.5f;

    [Header("スキル獲得")]
    [SerializeField] private SkillSelectView _skillSelectView;
    [SerializeField, Min(1)] private int _skillSelectCount = 3;

    [Header("シークエンス設定")]
    [SerializeField] private SequenceStateType _nextSequence = SequenceStateType.MobAndSkill;

    [Header("パネル表示中に停止する対象")]
    [SerializeField] private HitStopTargetGroup _pauseTargetGroup = HitStopTargetGroup.All;

    private enum Phase { Battle, WaitingForSkillGuide, SkillSelect }
    private enum TutorialStep { BasicOperations, WarriorExplanation, WarriorCombat, WaitingModeChange, WaitingModeChangeComplete, ThunderExplanation, ThunderCombat }

    private readonly Queue<TutorialPage> _pagesToShow = new();
    private SequenceStateContext _context;
    private WaveController _waveController;
    private SkillSelectPresenter _skillSelectPresenter;
    private CameraManager _cameraManager;
    private IDisposable _pauseHandle;
    private Phase _phase;
    private TutorialStep _step;
    private TutorialTrigger _activeTrigger;
    private bool _panelIsOpen;
    private bool _waveClearDetected;
    private bool _defeatGuideShown;
    private bool _defeatGuidePending;
    private bool _skillSelected;
    private bool _transitionRequested;
    private bool _armorBrokenWhilePanelOpen;
    private float _moveElapsed;
    private float _cameraMoveElapsed;
    private bool _dodgeCompleted;
    private bool _lockOnCompleted;
    private bool _attackCompleted;

    /// <summary>移動と視点操作を同じパネル上で並行判定する。</summary>
    private void TickBasicOperations(float deltaTime)
    {
        if (_context.InputHandler.MoveInput.sqrMagnitude > 0.01f)
            _moveElapsed = Mathf.Min(_moveDuration, _moveElapsed + deltaTime);
        if (_context.InputHandler.CameraMoveInput.sqrMagnitude > 0.01f)
            _cameraMoveElapsed = Mathf.Min(_cameraMoveDuration, _cameraMoveElapsed + deltaTime);
        UpdateBasicOperationProgress();
    }

    /// <summary>最初のウェーブをチュートリアル用として生成する。</summary>
    private void StartTutorialWave()
    {
        if (_waveSequenceData == null || _waveSequenceData.Waves == null || _waveSequenceData.Waves.Count == 0 || _spawnPointSelector == null)
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

    /// <summary>画面端の非モーダルパネルを表示する。</summary>
    private void ShowRealtimePage(TutorialTrigger trigger)
    {
        TutorialPage page = GetPage(trigger);
        if (_checklistView == null || page == null)
            return;
        _checklistView.ShowBasicOperations(page.Title);
        UpdateBasicOperationProgress();
    }

    /// <summary>ゲームを停止する説明ページをインスペクター設定のまま表示する。</summary>
    private void ShowModalPages(TutorialTrigger trigger)
    {
        _pagesToShow.Clear();
        _activeTrigger = trigger;
        if (_pages != null)
        {
            foreach (TutorialPage page in _pages)
                if (page != null && page.Trigger == trigger)
                    _pagesToShow.Enqueue(page);
        }

        if (_panelView == null || _pagesToShow.Count == 0)
        {
            CompleteModalGuide(trigger);
            return;
        }

        _panelIsOpen = true;
        BeginPause();
        _panelView.Show(_pagesToShow.Dequeue(), true);
    }

    /// <summary>時間停止パネルの次へボタンを処理する。</summary>
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
        CompleteModalGuide(_activeTrigger);
    }

    /// <summary>説明を閉じた後、仕様上の次段階へ移る。</summary>
    private void CompleteModalGuide(TutorialTrigger trigger)
    {
        switch (trigger)
        {
            case TutorialTrigger.ModeChange:
                _step = TutorialStep.WarriorCombat;
                ResumeBattle();
                if (_armorBrokenWhilePanelOpen)
                    StartModeChangeGuide();
                break;
            case TutorialTrigger.ThunderModeChanged:
                _step = TutorialStep.ThunderCombat;
                ResumeBattle();
                if (_defeatGuidePending)
                {
                    _defeatGuidePending = false;
                    ShowModalPages(TutorialTrigger.FirstEnemyDefeated);
                }
                break;
            case TutorialTrigger.FirstEnemyDefeated:
                _defeatGuideShown = true;
                ResumeBattle();
                break;
            case TutorialTrigger.WaveCleared:
                StartSkillSelect();
                break;
        }
    }

    /// <summary>回避を基本操作チェックへ反映する。</summary>
    private void HandleDodge()
    {
        if (_step != TutorialStep.BasicOperations)
            return;
        _dodgeCompleted = true;
        UpdateBasicOperationProgress();
    }

    /// <summary>最初の攻撃命中を契機に闘神説明を開く。</summary>
    private void HandleAttackHit(PlayerMode mode, ChargeLevel chargeLevel)
    {
        if (_step != TutorialStep.BasicOperations)
            return;
        _attackCompleted = true;
        UpdateBasicOperationProgress();
        _checklistView?.Hide();
        _step = TutorialStep.WarriorExplanation;
        ShowModalPages(TutorialTrigger.ModeChange);
    }

    /// <summary>鎧破壊後、モードチェンジ専用の操作待ちへ移る。</summary>
    private void HandleArmorBroken()
    {
        if (_step == TutorialStep.WarriorExplanation)
        {
            _armorBrokenWhilePanelOpen = true;
            return;
        }
        if (_step == TutorialStep.WarriorCombat)
            StartModeChangeGuide();
    }

    /// <summary>時間を止めたままモードチェンジ入力だけを受け付ける。</summary>
    private void StartModeChangeGuide()
    {
        _step = TutorialStep.WaitingModeChange;
        TutorialPage page = GetPage(TutorialTrigger.LockOn);
        if (_checklistView != null && page != null)
            _checklistView.ShowModeChange(page.Title, page.Description);
        BeginModeChangePause();
    }

    /// <summary>雷神への切り替え成立後、演出を進めるため時間停止だけを解除する。</summary>
    private void HandleModeChanged(PlayerMode mode)
    {
        if (_step != TutorialStep.WaitingModeChange || mode != PlayerMode.Thunder)
            return;
        _checklistView?.CompleteModeChange();
        _checklistView?.Hide();
        _context.Player.SetTutorialModeChangeEnabled(false);
        ReleasePause();
        _step = TutorialStep.WaitingModeChangeComplete;
    }

    /// <summary>モードチェンジ演出が完了して操作可能になってから、雷神モードの説明を開く。</summary>
    private void HandleModeChangeCompleted()
    {
        if (_step != TutorialStep.WaitingModeChangeComplete)
            return;

        _step = TutorialStep.ThunderExplanation;
        ShowModalPages(TutorialTrigger.ThunderModeChanged);
    }

    /// <summary>ロックオン成立を基本操作チェックへ反映する。</summary>
    private void HandleLockOnTargetChanged(ILockOnTarget target)
    {
        if (_step != TutorialStep.BasicOperations || target == null)
            return;
        _lockOnCompleted = true;
        UpdateBasicOperationProgress();
    }

    /// <summary>敵撃破とウェーブ完了を記録し、雷神段階で経験値説明を開く。段階到達前の撃破は保留する。</summary>
    private void HandleEnemyDefeated()
    {
        _waveController?.OnEnemyDefeated();
        if (_waveController != null && _waveController.IsComplete)
            _waveClearDetected = true;

        if (_defeatGuideShown)
            return;

        if (_step == TutorialStep.ThunderCombat)
            ShowModalPages(TutorialTrigger.FirstEnemyDefeated);
        else
            _defeatGuidePending = true;
    }

    /// <summary>STEP 1の全項目を一枚のチェックリストとして更新する。</summary>
    private void UpdateBasicOperationProgress()
    {
        if (_checklistView == null || _step != TutorialStep.BasicOperations)
            return;
        _checklistView.SetBasicOperationProgress(
            _moveElapsed >= _moveDuration,
            _cameraMoveElapsed >= _cameraMoveDuration,
            _lockOnCompleted,
            _dodgeCompleted,
            _attackCompleted);
    }

    /// <summary>スキル三択を開く。</summary>
    private void StartSkillSelect()
    {
        _phase = Phase.SkillSelect;
        _context.InputHandler.EnableInput(false);
        ShowCursor();
        BeginPause();
        if (_skillSelectView == null)
        {
            Debug.LogWarning("[TutorialState] SkillSelectView が未設定のため、スキル選択をスキップします。");
            FinishTutorial();
            return;
        }
        _skillSelectPresenter = new SkillSelectPresenter(_context.SkillManager, _skillSelectView, _context.Player);
        if (!_skillSelectPresenter.Open(_skillSelectCount))
        {
            FinishTutorial();
            return;
        }
        _skillSelectView.OnSkillSelected += HandleSkillSelected;
    }

    /// <summary>スキル選択完了を一度だけ受理する。</summary>
    private void HandleSkillSelected(int _)
    {
        if (_skillSelected)
            return;
        _skillSelected = true;
        FinishTutorial();
    }

    /// <summary>チュートリアルを終了し次シークエンスへの遷移を要求する。</summary>
    private void FinishTutorial()
    {
        if (_skillSelectView != null)
            _skillSelectView.OnSkillSelected -= HandleSkillSelected;
        _skillSelectPresenter?.Dispose();
        _skillSelectPresenter = null;
        ReleasePause();
        _transitionRequested = true;
    }

    /// <summary>説明パネル用にゲーム全体と入力を停止する。</summary>
    private void BeginPause()
    {
        ReleasePause();
        _context.InputHandler.EnableInput(false);
        ShowCursor();
        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
            _pauseHandle = hitStopManager.BeginManualStop(_pauseTargetGroup);
    }

    /// <summary>モードチェンジだけ可能な状態でゲーム時間を停止する。</summary>
    private void BeginModeChangePause()
    {
        ReleasePause();
        _context.InputHandler.EnableInput(true);
        _context.InputHandler.SetLockOnEnabled(false);
        _context.InputHandler.SetModeChangeEnabled(true);
        // 鎧破壊は攻撃ヒット中に発生するため、停止した攻撃ステートのままでも切り替えを受理させる。
        _context.Player.SetTutorialModeChangeEnabled(true);
        HideCursor();
        if (ServiceLocator.TryGet(out HitStopManager hitStopManager))
            _pauseHandle = hitStopManager.BeginManualStop(_pauseTargetGroup);
    }

    /// <summary>手動の時間停止を解除する。</summary>
    private void ReleasePause()
    {
        _pauseHandle?.Dispose();
        _pauseHandle = null;
    }

    /// <summary>戦闘入力とカーソルを通常へ戻す。</summary>
    private void ResumeBattle()
    {
        _phase = Phase.Battle;
        _context.InputHandler.SetLockOnEnabled(true);
        _context.InputHandler.EnableInput(true);
        HideCursor();
    }

    /// <summary>基本操作チェックを初期化する。</summary>
    private void ResetBasicOperationProgress()
    {
        _moveElapsed = 0f;
        _cameraMoveElapsed = 0f;
        _dodgeCompleted = false;
        _lockOnCompleted = false;
        _attackCompleted = false;
        _armorBrokenWhilePanelOpen = false;
    }

    /// <summary>指定トリガーに対応する最初の設定ページを取得する。</summary>
    private TutorialPage GetPage(TutorialTrigger trigger)
    {
        if (_pages == null)
            return null;
        foreach (TutorialPage page in _pages)
            if (page != null && page.Trigger == trigger)
                return page;
        return null;
    }

    private static void HideCursor() => Cursor.visible = false;

    private static void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
