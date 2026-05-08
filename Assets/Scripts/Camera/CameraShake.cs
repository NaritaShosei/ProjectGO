using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using Unity.Cinemachine;

public struct CameraShakeData
{
    public CinemachineCamera playerCamera;
    public float amplitude;
    public float frequency;
    public float duration;
}

public class CameraShake
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="playercamera">振動させるカメラ</param>
    /// <param name="amplitude"></param>
    /// <param name="frequency"></param>
    /// <param name="duration">持続時間</param>
    public CameraShake(CinemachineCamera playerCamera)
    {
        _noise = playerCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    /// <summary>
    /// CameraShakeを開始
    /// </summary>
    public async Task StartCameraShake()
    {
        if (_noise == null) return;

        CameraShakeData data = new CameraShakeData();

        _noise.AmplitudeGain = data.amplitude;
        _noise.FrequencyGain = data.frequency;

        await StopCameraShake(data);
    }

    private CinemachineBasicMultiChannelPerlin _noise;

    /// <summary>
    /// CameraShakeを停止
    /// </summary>
    private async UniTask StopCameraShake(CameraShakeData data)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(data.duration));

        _noise.AmplitudeGain = 0;
        _noise.FrequencyGain = 0;
    }
}
