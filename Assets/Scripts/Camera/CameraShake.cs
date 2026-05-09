using Cysharp.Threading.Tasks;
using System;
using Unity.Cinemachine;

public struct CameraShakeData
{
    public float amplitude;
    public float frequency;
    public float duration;
}

public class CameraShake
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    public CameraShake(CinemachineCamera playerCamera)
    {
        if (playerCamera == null) return;

        _noise = playerCamera.GetCinemachineComponent(CinemachineCore.Stage.Noise)
                 as CinemachineBasicMultiChannelPerlin;
    }

    /// <summary>
    /// CameraShakeを開始
    /// </summary>
    public async UniTask StartCameraShake(CameraShakeData data)
    {
        if (_noise == null) return;

        int shakeRequestNumber = ++_shakeRequestRestrictionNumber;

        _noise.AmplitudeGain = data.amplitude;
        _noise.FrequencyGain = data.frequency;

        await StopCameraShake(data, shakeRequestNumber);
    }

    private CinemachineBasicMultiChannelPerlin _noise;
    private int _shakeRequestRestrictionNumber = 0;

    /// <summary>
    /// CameraShakeを停止
    /// </summary>
    private async UniTask StopCameraShake(CameraShakeData data, int shakeRequestNumber)
    {
        if (_shakeRequestRestrictionNumber != shakeRequestNumber) return;

        await UniTask.Delay(TimeSpan.FromSeconds(data.duration));

        _noise.AmplitudeGain = 0;
        _noise.FrequencyGain = 0;
    }
}
