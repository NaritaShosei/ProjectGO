using UnityEngine;

/// <summary>
/// エフェクトの共通基底クラス。
/// テンプレートメソッドパターンで TimeScale 適用・ライフサイクルを統一し、
/// 具体的な再生制御は派生クラスに委譲する。
/// </summary>
public abstract class EffectBase : MonoBehaviour, ISpeedChange, IPoolable
{
    // ── プロパティ ─────────────────────────────────────────────
    public float TimeScale => _timeScale;

    // ── 公開 API ──────────────────────────────────────────────

    /// <summary>エフェクトを再生する。</summary>
    public void Play()
    {
        OnPlayInternal();
    }

    /// <summary>エフェクトを停止し、状態をクリアする。</summary>
    public void Stop()
    {
        OnStopInternal();
    }

    /// <summary>エフェクトがまだ生存中かどうかを返す。</summary>
    public bool IsAlive()
    {
        return IsAliveInternal();
    }

    /// <summary> エフェクトのスケールを設定する。</summary>
    public void SetScale(Vector3 scale)
    {
        ApplyScaleInternal(scale);
    }

    // ── IPoolable ────────────────────────────────────────────

    /// <summary>プールから取り出された直後に呼ばれる。</summary>
    public virtual void OnGet() { }

    /// <summary>プールへ返却される直前に呼ばれる。状態を完全にリセットする。</summary>
    public virtual void OnRelease()
    {
        Stop();
        _timeScale = 1f;
        ApplyTimeScaleInternal(TimeScale);
        ApplyScaleInternal(Vector3.one);
    }

    // ── ISpeedChange ──────────────────────────────────────────

    public void OnSpeedChange(float scale)
    {
        _timeScale = scale;
        ApplyTimeScaleInternal(TimeScale);
    }

    // ── 派生クラスへの委譲 (abstract) ─────────────────────────

    /// <summary>再生開始の具体的な処理。</summary>
    protected abstract void OnPlayInternal();

    /// <summary>停止・クリアの具体的な処理。</summary>
    protected abstract void OnStopInternal();

    /// <summary>生存判定の具体的なロジック。</summary>
    protected abstract bool IsAliveInternal();

    /// <summary>スケールを実際のコンポーネントに適用する。</summary>
    protected abstract void ApplyScaleInternal(Vector3 scale);

    /// <summary>
    /// タイムスケールを実際のコンポーネントに適用する。
    /// OnRelease・OnSpeedChange の両方から呼ばれる。
    /// </summary>
    protected abstract void ApplyTimeScaleInternal(float scale);

    // ── Unity ライフサイクルフック (任意でオーバーライド可) ───

    protected virtual void Awake() { }

    protected float _timeScale = 1f;
}
