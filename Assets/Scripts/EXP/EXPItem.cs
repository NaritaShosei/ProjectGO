using System;
using UnityEngine;

public class EXPItem : MonoBehaviour, ISpeedChange, IPoolable
{
    private const float INTERACT_RANGE = 0.1f;

    public event Action<EXPItem> OnReleased;

    public float TimeScale => _timeScale;

    // ── IPoolable ────────────────────────────────────────────

    /// <summary>プールから取り出された直後。TimeScale を初期化する。</summary>
    public void OnGet()
    {
        _timeScale = 1f;
        OnReleased = null;
    }

    /// <summary>プールへ返却される直前。特別な処理は不要。</summary>
    public void OnRelease() { }

    // ── 既存 API ─────────────────────────────────────────────

    public void Tick(IPlayer player, float magnetRange)
    {
        Vector3 playerCenterPos = player.GetTargetCenter().position;

        float distanceToPlayer = Vector3.Distance(transform.position, playerCenterPos);
        if (distanceToPlayer <= magnetRange)
        {
            Vector3 direction = (playerCenterPos - transform.position).normalized;

            float t = Mathf.Clamp01(1f - (distanceToPlayer / magnetRange));
            float speed = Mathf.Lerp(0f, 10f, t);
            transform.position += direction * speed * Time.deltaTime * TimeScale;

            if (distanceToPlayer <= INTERACT_RANGE)
            {
                Interact();
            }
        }
    }

    public void Interact()
    {
        if (ServiceLocator.TryGet(out EXPManager expManager))
        {
            expManager.AddEXP(_expValue);
        }

        OnReleased?.Invoke(this);
    }

    public void OnSpeedChange(float scale)
    {
        _timeScale = scale;
    }

    [SerializeField] private float _expValue;
    private float _timeScale = 1f;
}
