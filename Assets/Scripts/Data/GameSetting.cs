using System;
using UnityEngine;

[Serializable]
public class GameSetting
{
    // プロパティ
    public bool IsControllerVibrations
    {
        get => isControllerVibrations;
        set => isControllerVibrations = value;
    }

    public float CameraMoveSpeed
    {
        get => cameraMoveSpeed;
        set => cameraMoveSpeed = Mathf.Clamp01(value);
    }

    public float CameraRotationSensitivity
    {
        get => cameraRotationSensitivity;
        set => cameraRotationSensitivity = Mathf.Clamp01(value);
    }

    public float BGMVolume
    {
        get => bgmVolume;
        set => bgmVolume = Mathf.Clamp01(value);
    }

    public float SEVolume
    {
        get => seVolume;
        set => seVolume = Mathf.Clamp01(value);
    }

    public float VoiceVolume
    {
        get => voiceVolume;
        set => voiceVolume = Mathf.Clamp01(value);
    }

    public GameSetting Clone()
    {
        return (GameSetting)this.MemberwiseClone();
    }

    // 静的な読み取り専用デフォルト値
    public static readonly GameSetting Default = new GameSetting();

    [Header("コントローラー設定")]
    [SerializeField] private bool isControllerVibrations = true;
    [SerializeField, Range(0f, 1f)] private float cameraMoveSpeed = 0.5f;
    [SerializeField, Range(0f, 1f)] private float cameraRotationSensitivity = 0.5f;

    [Header("音量設定")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float seVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float voiceVolume = 0.5f;
}