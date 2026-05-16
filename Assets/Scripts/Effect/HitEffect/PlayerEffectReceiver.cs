using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerEffectReceiver : MonoBehaviour
{
    private Player _player;
    private EffectManager _effectManager;
    private Vignette _vignette;

    [Header("Damage Effects")]
    [SerializeField] private string _damageEffectKey = "player_hit";

    [Header("Screen Effects")]
    [SerializeField] private Volume _volume;
    [SerializeField] private float _flashDuration = 0.15f;
    [SerializeField] private float _flashAlpha = 0.5f;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _effectManager = ServiceLocator.Get<EffectManager>();

        if (_player == null)
        {
            Debug.LogError("Player が見つかりません", this);
            enabled = false;
            return;
        }

        if (_volume.profile.TryGet(out _vignette))
        {
            _vignette.intensity.value = 0f;
        }

        _player.OnDamagedEffect += HandleDamaged;
    }

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnDamagedEffect -= HandleDamaged;
        }
    }

    private void HandleDamaged(PlayerDamageEffectContext context)
    {
        _effectManager.PlayEffect(
            _damageEffectKey,
            context.HitPosition);

        PlayerDamageFlash().Forget();
    }

    private async UniTaskVoid PlayerDamageFlash()
    {
        if (_volume != null) return;

        _vignette.intensity.value = _flashAlpha;

        while (_vignette.intensity.value > 0f)
        {
            _vignette.intensity.value -= Time.deltaTime / _flashDuration * _flashAlpha;
            await UniTask.Yield();
        }

        _vignette.intensity.value = 0f;
    }
}
