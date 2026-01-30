using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class OptionView : MonoBehaviour
{
    //戻るボタン回り
    public event Action OnBackButtonClicked;
    public event Action OnResetButtonClicked;
    public event Action OnSaveButtonClicked;

    //コントローラー設定
    public event Action<bool> OnControllerVibrationsToggleChanged;
    public event Action<float> OnCameraMoveSpeedSliderChanged;
    public event Action<float> OnCameraRotationSensitivitySliderChanged;

    //音量設定
    public event Action<float> OnBGMVolumeSliderChanged;
    public event Action<float> OnSEVolumeSliderChanged;
    public event Action<float> OnVoiceVolumeSliderChanged;

    // Presenter向けの設定適用メソッド
    public void SetControllerVibrations(bool isOn) => _controllerVibrationsToggle.isOn = isOn;
    public void SetCameraMoveSpeed(float value) => _cameraMoveSpeedSlider.value = value;
    public void SetCameraRotationSensitivity(float value) => _cameraRotationSensitivitySlider.value = value;
    public void SetBGMVolume(float value) => _bgmVolumeSlider.value = value;
    public void SetSEVolume(float value) => _seVolumeSlider.value = value;
    public void SetVoiceVolume(float value) => _voiceVolumeSlider.value = value;

    [Header("戻るボタン回り")]
    [SerializeField]
    private Button _backButton;
    [SerializeField]
    private Button _ResetButton;
    [SerializeField]
    private Button _SaveButton;

    [Header("コントローラー設定")]
    [SerializeField]
    private Toggle _controllerVibrationsToggle;
    [SerializeField]
    private Slider _cameraMoveSpeedSlider;
    [SerializeField]
    private Slider _cameraRotationSensitivitySlider;

    [Header("音量設定")]
    [SerializeField]
    private Slider _bgmVolumeSlider;
    [SerializeField]
    private Slider _seVolumeSlider;
    [SerializeField]
    private Slider _voiceVolumeSlider;

    private void Start()
    {
        //戻るボタン回り
        _backButton.onClick.AddListener(() => OnBackButtonClicked?.Invoke());
        _ResetButton.onClick.AddListener(() => OnResetButtonClicked?.Invoke());
        _SaveButton.onClick.AddListener(() => OnSaveButtonClicked?.Invoke());

        //コントローラー設定
        _controllerVibrationsToggle.onValueChanged.AddListener((value) => OnControllerVibrationsToggleChanged?.Invoke(value));
        _cameraMoveSpeedSlider.onValueChanged.AddListener((value) => OnCameraMoveSpeedSliderChanged?.Invoke(value));
        _cameraRotationSensitivitySlider.onValueChanged.AddListener((value) => OnCameraRotationSensitivitySliderChanged?.Invoke(value));

        //音量設定
        _bgmVolumeSlider.onValueChanged.AddListener((value) => OnBGMVolumeSliderChanged?.Invoke(value));
        _seVolumeSlider.onValueChanged.AddListener((value) => OnSEVolumeSliderChanged?.Invoke(value));
        _voiceVolumeSlider.onValueChanged.AddListener((value) => OnVoiceVolumeSliderChanged?.Invoke(value));

        ShowThisPanel();
    }

    private void ShowThisPanel()
    {
        EventSystem.current.SetSelectedGameObject(_controllerVibrationsToggle.gameObject);
    }

    private void OnDestroy()
    {
        //戻るボタン回り
        _backButton.onClick.RemoveAllListeners();
        _ResetButton.onClick.RemoveAllListeners();
        _SaveButton.onClick.RemoveAllListeners();
        //コントローラー設定
        _controllerVibrationsToggle.onValueChanged.RemoveAllListeners();
        _cameraMoveSpeedSlider.onValueChanged.RemoveAllListeners();
        _cameraRotationSensitivitySlider.onValueChanged.RemoveAllListeners();
        //音量設定
        _bgmVolumeSlider.onValueChanged.RemoveAllListeners();
        _seVolumeSlider.onValueChanged.RemoveAllListeners();
        _voiceVolumeSlider.onValueChanged.RemoveAllListeners();
    }
}
