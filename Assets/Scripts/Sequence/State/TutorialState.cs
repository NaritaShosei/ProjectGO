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
        _defeatGuidePending = false;
        _waveClearDetected = false;
        _skillSelected = false;
        _transitionRequested = false;

        context.InputHandler?.EnableInput(false);
        context.EnemyManager.OnEnemyDefeated += HandleEnemyDefeated;

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

        if (!_realtimeGuideActive && _defeatGuidePending)
        {
            _defeatGuidePending = false;
            ShowPages(TutorialTrigger.FirstEnemyDefeated);
            return null;
        }

        _waveController?.Tick();

        bool waveComplete = _waveClearDetected ||
            (_waveController != null && _waveController.IsComplete);

        if (waveComplete && _realtimeGuideActive)
        {
            _waveClearDetected = true;
            return null;
        }

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
    private IDisposable _pauseHandle;
    private Phase _phase;
    private TutorialTrigger _activeTrigger;
    private bool _panelIsOpen;
    private bool _defeatGuideShown;
    private bool _defeatGuidePending;
    private bool _waveClearDetected;
    private bool _skillSelected;
    private bool _transitionRequested;
    private bool _realtimeGuideActive;
    private float _realtimeGuideRemaining;

    private void HandleEnemyDefeated()
    {
        _waveController?.OnEnemyDefeated();

        if (!_defeatGuideShown)
        {
            _defeatGuideShown = true;
            if (_realtimeGuideActive)
            {
                _defeatGuidePending = true;
            }
            else
            {
                ShowPages(TutorialTrigger.FirstEnemyDefeated);
            }
        }

        if (_waveController != null && _waveController.IsComplete)
            _waveClearDetected = true;
    }

    private void ShowPages(TutorialTrigger trigger)
    {
        EndRealtimeGuide();
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
                ResumeBattle();
                break;
            case TutorialTrigger.WaveCleared:
                StartSkillSelect();
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
            _realtimeGuideRemaining = page.Duration;
            _panelView.Show(page, false);
            return;
        }

        AdvanceRealtimeGuide();
    }

    private void TickRealtimeGuide(float deltaTime)
    {
        if (!_realtimeGuideActive)
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
                StartRealtimeGuide(TutorialTrigger.ModeChange);
                break;
            case TutorialTrigger.ModeChange:
                StartRealtimeGuide(TutorialTrigger.LockOn);
                break;
            default:
                EndRealtimeGuide();
                break;
        }
    }

    private void EndRealtimeGuide()
    {
        _realtimeGuideActive = false;
        _realtimeGuideRemaining = 0f;
        _pagesToShow.Clear();
        _panelView?.Hide();
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
