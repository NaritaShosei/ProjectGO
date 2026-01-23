using System;
using UnityEngine;

public class ModeSelectPresenter : IDisposable
{
    public event Action OnModeSelectCloseRequested;
    public event Action<string> OnSceneSelected;

    public ModeSelectPresenter(ModeSelectView view, ModeSelectModel model)
    {
        this._modeSelectView = view;
        this._modeSelectModel = model;

        // ハンドラを作成して保持
        inGameHandler = () => HandleModeSelect(_modeSelectModel.InGameModeSceneName);
        bossHandler = () => HandleModeSelect(_modeSelectModel.BossModeSceneName);
        practiceHandler = () => HandleModeSelect(_modeSelectModel.PracticeModeSceneName);
        backHandler = () => OnModeSelectCloseRequested?.Invoke();

        // イベント登録
        _modeSelectView.OnInGameModeButton += inGameHandler;
        _modeSelectView.OnBossModeButton += bossHandler;
        _modeSelectView.OnPracticeModeButton += practiceHandler;
        _modeSelectView.OnBackButton += backHandler;

        Debug.Log("[ModeSelectPresenter] 初期化完了");
    }

    public void Dispose()
    {
        Debug.Log("[ModeSelectPresenter] Presenter破棄");

        // イベント解除
        _modeSelectView.OnInGameModeButton -= inGameHandler;
        _modeSelectView.OnBossModeButton -= bossHandler;
        _modeSelectView.OnPracticeModeButton -= practiceHandler;
        _modeSelectView.OnBackButton -= backHandler;
    }

    private ModeSelectView _modeSelectView;
    private ModeSelectModel _modeSelectModel;

    // イベントハンドラを保持
    private Action inGameHandler;
    private Action bossHandler;
    private Action practiceHandler;
    private Action backHandler;

    // 共通のハンドラ
    private void HandleModeSelect(string sceneName)
    {
        Debug.Log($"[ModeSelectPresenter] シーン選択: {sceneName}");
        OnSceneSelected?.Invoke(sceneName);
    }
}