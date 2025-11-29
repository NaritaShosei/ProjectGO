using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModeManager : MonoBehaviour
{
    [Header("モードデータ")]
    [SerializeField] private List<PlayerModeData> _modeDataList = new();

    private PlayerManager _manager;
    private PlayerAttacker _attacker;
    private InputHandler _input;

    private int _currentModeIndex = 0;
    private PlayerMode _currentMode;

    public PlayerMode CurrentMode => _currentMode;
    public PlayerModeData CurrentModeData => _currentMode?.Data;

    public event Action<PlayerMode> OnModeChanged;

    private void Awake()
    {
        if (_modeDataList.Count == 0)
        {
            Debug.LogError("モードデータが設定されていません");
            return;
        }
    }

    public void Init(PlayerManager manager, PlayerAttacker attacker, InputHandler input)
    {
        _manager = manager;
        _attacker = attacker;
        _input = input;

        _input.OnModeChange += TrySwitchMode;

        SetMode(0);
    }

    private void OnDisable()
    {
        if (_input != null)
        {
            _input.OnModeChange -= TrySwitchMode;
        }
    }

    private void Update()
    {
        _currentMode?.OnUpdate();
    }

    private void TrySwitchMode()
    {
        if (!CanSwitchMode()) return;

        _currentModeIndex++;

        int nextIndex = _currentModeIndex % _modeDataList.Count;
        SetMode(nextIndex);
    }

    private bool CanSwitchMode()
    {
        if (_manager == null) return false;

        return !_manager.HasFlag(
            PlayerStateFlags.Dead |
            PlayerStateFlags.Attacking |
            PlayerStateFlags.Dodging |
            PlayerStateFlags.MoveLocked
        );
    }

    private void SetMode(int index)
    {
        if (index < 0 || index >= _modeDataList.Count) return;

        var data = _modeDataList[index];

        // 前のモード終了
        _currentMode?.OnExit();

        // 新しいモード生成
        _currentMode = CreateMode(data.ModeType);
        _currentMode.Init(_manager, _attacker, data);

        _currentModeIndex = index;

        // 新しいモード開始
        _currentMode.OnEnter();

        // イベント発火
        OnModeChanged?.Invoke(_currentMode);

        Debug.Log($"モード切替: {data.ModeName}");
    }

    private PlayerMode CreateMode(ModeType type)
    {
        return type switch
        {
            ModeType.ThunderGod => new ThunderGodMode(),
            ModeType.BattleGod => new BattleGodMode(),
            _ => new BattleGodMode()
        };
    }

    // 外部から特定のモードクラスを取得
    public T GetCurrentModeAs<T>() where T : PlayerMode
    {
        return _currentMode as T;
    }
}