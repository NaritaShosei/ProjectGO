using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;

[Serializable]
public struct CameraShakeData
{
    ///<summary>振幅</summary>
    public float Amplitude => _amplitude;
    ///<summary>周期</summary>
    public float Frequency => _frequency;
    ///<summary>持続時間</summary>
    public float Duration => _duration;

    [SerializeField] private float _amplitude;
    [SerializeField] private float _frequency;
    [SerializeField] private float _duration;
}

public class CameraShake
{
    /// <summary>
    /// CameraShakeを開始
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public async UniTask StartCameraShake(CinemachineCamera camera,CameraShakeData data)
    {
        _noise = camera.GetCinemachineComponent(CinemachineCore.Stage.Noise)
            as CinemachineBasicMultiChannelPerlin;

        if (_noise == null)
        {
            Debug.LogWarning($"現在使用中のカメラ{camera.name} に CinemachineBasicMultiChannelPerlinがアタッチされていません。");
            return;
        }

        ForceStopCameraShake();

        _shakeCts = new CancellationTokenSource();

        _noise.AmplitudeGain = data.Amplitude;
        _noise.FrequencyGain = data.Frequency;

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
            await UniTask.Delay(TimeSpan.FromSeconds(data.Duration), cancellationToken: cancellationToken);
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
