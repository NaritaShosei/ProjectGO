using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public void Shake(float amplitude, float frequency, float duration)
    {
        if (_noise == null) return;

        _noise.AmplitudeGain = amplitude;
        _noise.FrequencyGain = frequency;

        Invoke(nameof(StopShake), duration);
    }

    private void Awake()
    {
        _noise = _virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
    }
    
    private void StopShake()
    {
        _noise.AmplitudeGain = 0f;
        _noise.FrequencyGain = 0f;
    }

    [SerializeField] private CinemachineCamera _virtualCamera;

    private CinemachineBasicMultiChannelPerlin _noise;
}
