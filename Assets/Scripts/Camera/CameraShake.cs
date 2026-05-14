using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Cinemachine;

public struct CameraShakeData
{
    public float amplitude;       ///<summary>振幅</summary>
    public float frequency;       ///<summary>周期</summary>
    public float duration;        ///<summary>持続時間</summary>
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
    /// <param name="data"></param>
    /// <returns></returns>
    public async UniTask StartCameraShake(CameraShakeData data)
    {
        if (_noise == null) return;

        ForceStopCameraShake();

        _shakeCts = new CancellationTokenSource();

        _noise.AmplitudeGain = data.amplitude;
        _noise.FrequencyGain = data.frequency;

        await StopCameraShake(data, _shakeCts.Token);
    }

    /// <summary>
    /// カメラシェイクの強制停止
    /// </summary>
    public void ForceStopCameraShake()
    {
        if (_shakeCts == null) return;

        _shakeCts.Cancel();
        _shakeCts.Dispose();
        _shakeCts = null;
    }

    private CinemachineBasicMultiChannelPerlin _noise;
    private CancellationTokenSource _shakeCts;

    /// <summary>
    /// CameraShakeを停止
    /// </summary>
    private async UniTask StopCameraShake(CameraShakeData data, CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(data.duration), cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            if (_noise != null)
            {
                _noise.AmplitudeGain = 0;
                _noise.FrequencyGain = 0;
            }
        }
    }
}
