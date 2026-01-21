using System;
using UnityEngine;

public class OptionPresenter
{
    // 保存リクエスト。UIManagerが使う、なお未実装
    public event Action<GameSetting> OnSettingsSaved;
    // 閉じるリクエスト、UIManegerが使う
    public event Action OnCloseRequested;
    public OptionPresenter(OptionView view, OptionModel model)
    {
        this._optionView = view;
        this._optionModel = model;

        // ハンドラを作成
        _vibrationHandler = v => { _tempSettings.IsControllerVibrations = v; LogChange("振動", v ? "ON" : "OFF"); };
        _cameraMoveHandler = v => { _tempSettings.CameraMoveSpeed = v; LogChange("カメラ移動速度", v); };
        _cameraRotationHandler = v => { _tempSettings.CameraRotationSensitivity = v; LogChange("カメラ回転感度", v); };
       
        _bgmHandler = v => { _tempSettings.BGMVolume = v; LogChange("BGM音量", v); };
        _seHandler = v => { _tempSettings.SEVolume = v; LogChange("SE音量", v); };
        _voiceHandler = v => { _tempSettings.VoiceVolume = v; LogChange("Voice音量", v); };

        // イベント登録
        view.OnSaveButtonClicked += HandleSave;
        view.OnBackButtonClicked += HandleBack;
        view.OnResetButtonClicked += HandleReset;

        view.OnControllerVibrationsToggleChanged += _vibrationHandler;
        view.OnCameraMoveSpeedSliderChanged += _cameraMoveHandler;
        view.OnCameraRotationSensitivitySliderChanged += _cameraRotationHandler;

        view.OnBGMVolumeSliderChanged += _bgmHandler;
        view.OnSEVolumeSliderChanged += _seHandler;
        view.OnVoiceVolumeSliderChanged += _voiceHandler;


        InitializeOptionPanel();
    }


    private OptionView _optionView;
    private OptionModel _optionModel;
    private GameSetting _tempSettings; // 一時的な編集データ

    // イベントハンドラの保持（登録解除用）
    private Action<float> _bgmHandler;
    private Action<float> _seHandler;
    private Action<float> _voiceHandler;
    private Action<float> _cameraMoveHandler;
    private Action<float> _cameraRotationHandler;
    private Action<bool> _vibrationHandler;


    private void InitializeOptionPanel()
    {
        // Modelから現在の設定を取得して一時データにコピー
        _tempSettings = _optionModel.CurrentGameSettings.Clone();

        // UIに反映
        ApplyToView();

        Debug.Log("[OptionPresenter] 初期化完了");
        LogSettings("初期設定", _tempSettings);
    }

    private void HandleSave()
    {
        Debug.Log("[OptionPresenter] 保存ボタン押下");
        Debug.Log("[OptionPresenter] 保存前:");
        LogSettings("Model保存前", _optionModel.CurrentGameSettings);

        // Modelに保存
        _optionModel.Apply(_tempSettings);

        Debug.Log("[OptionPresenter] 保存後:");
        LogSettings("Model保存後", _optionModel.CurrentGameSettings);

        // ゲームシステムに反映するためのイベント発火
        OnSettingsSaved?.Invoke(_optionModel.CurrentGameSettings);
    }

    private void HandleBack()
    {
        Debug.Log("[OptionPresenter] 戻るボタン押下");
        Debug.Log("[OptionPresenter] 変更を破棄して閉じます");

        // 閉じるリクエストを送る（実際に閉じるのはControllerの仕事）
        OnCloseRequested?.Invoke();
    }

    private void HandleReset()
    {
        Debug.Log("[OptionPresenter] リセットボタン押下");

        // デフォルト値に戻す
        _tempSettings = GameSetting.GetDefault();

        // UIに反映
        ApplyToView();

        LogSettings("リセット後", _tempSettings);
    }

    private void ApplyToView()
    {
        _optionView.SetControllerVibrations(_tempSettings.IsControllerVibrations);
        _optionView.SetCameraMoveSpeed(_tempSettings.CameraMoveSpeed);
        _optionView.SetCameraRotationSensitivity(_tempSettings.CameraRotationSensitivity);
       
        _optionView.SetBGMVolume(_tempSettings.BGMVolume);
        _optionView.SetSEVolume(_tempSettings.SEVolume);
        _optionView.SetVoiceVolume(_tempSettings.VoiceVolume);

        Debug.Log("[OptionPresenter] Viewに反映");
    }

    public void Dispose()
    {
        Debug.Log("[OptionPresenter] Presenter破棄");

        // イベント登録解除
        _optionView.OnSaveButtonClicked -= HandleSave;
        _optionView.OnBackButtonClicked -= HandleBack;
        _optionView.OnResetButtonClicked -= HandleReset;

        _optionView.OnControllerVibrationsToggleChanged -= _vibrationHandler;
        _optionView.OnCameraMoveSpeedSliderChanged -= _cameraMoveHandler;
        _optionView.OnCameraRotationSensitivitySliderChanged -= _cameraRotationHandler;

        _optionView.OnBGMVolumeSliderChanged -= _bgmHandler;
        _optionView.OnSEVolumeSliderChanged -= _seHandler;
        _optionView.OnVoiceVolumeSliderChanged -= _voiceHandler;
    }

    // ========== デバッグログ ==========

    private void LogChange(string name, object value)
    {
        Debug.Log($"[OptionPresenter] {name}変更: {value}");
    }

    private void LogSettings(string label, GameSetting settings)
    {
        Debug.Log($"========== {label} ==========");
        Debug.Log($"  振動: {(settings.IsControllerVibrations ? "ON" : "OFF")}");
        Debug.Log($"  カメラ移動: {settings.CameraMoveSpeed:F2}");
        Debug.Log($"  カメラ回転: {settings.CameraRotationSensitivity:F2}");
        Debug.Log($"  BGM: {settings.BGMVolume:F2}");
        Debug.Log($"  SE: {settings.SEVolume:F2}");
        Debug.Log($"  Voice: {settings.VoiceVolume:F2}");
        Debug.Log("=====================================");
    }
}