using System;
using UnityEngine;

[Serializable]
public class GameSetting
{
    [Header("コントローラー設定")]
    public bool IsControllerVibrations = true;
    [Range(0f, 1f)] public float CameraMoveSpeed = 0.5f;
    [Range(0f, 1f)] public float CameraRotationSensitivity = 0.5f;

    [Header("音量設定")]
    [Range(0f, 1f)] public float BGMVolume = 0.5f;
    [Range(0f, 1f)] public float SEVolume = 0.5f;
    [Range(0f, 1f)] public float VoiceVolume = 0.5f;

    public GameSetting Clone()
    {
        return (GameSetting)this.MemberwiseClone();
    }

    public static GameSetting GetDefault()
    {
        return new GameSetting();
    }
}