using UnityEngine;
using UnityEngine.VFX;

public class VisualEffectContoller : EffectBase
{
    /// <summary>
    /// プールから取り出された瞬間に呼ばれる（SetActive(true)より前に呼ばれる可能性がある）
    /// </summary>
    public override void OnGet()
    {
        base.OnGet();

        // 原因対策2：プールから出た瞬間、外側が忘れていても強制的に自身をアクティブにする
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // 原因対策3：位置ズレ対策（再生前のフレームでVFXのシミュレーションが走るのを防ぐ）
        if (_rootVFX != null)
        {
            _rootVFX.enabled = true;
            _rootVFX.pause = true; // 位置が確定するまで一時停止させておく
        }
    }

    public override void OnRelease()
    {
        // プールに戻る際の後始末
        base.OnRelease();
        _isStarting = false;
    }

    [SerializeField] private VisualEffect _rootVFX;
    [SerializeField] private float _despawnTime = 2f;
    private VisualEffect[] _vfxs;
    private float _currentAliveTime = 0;

    // 生存判定のバグ（即回収）を防ぐためのフラグ
    private bool _isStarting = false;

    protected override void Awake()
    {
        // 念のため自分自身の初期化メソッドもベースを呼んでおく
        base.Awake();

        // 子オブジェクトを含めて全てのVisualEffectコンポーネントを収集
        _vfxs = GetComponentsInChildren<VisualEffect>(includeInactive: true);

        // 原因対策1：_rootVFXが未設定なら、自分自身から自動取得
        if (_rootVFX == null)
        {
            _rootVFX = GetComponent<VisualEffect>();
        }
    }

    protected override void OnPlayInternal()
    {
        if (_rootVFX != null)
        {
            Debug.Log("隕石");
            // 現在の生存期間を初期化
            _currentAliveTime = 0;

            // 原因対策3の続き：再生が呼ばれた＝位置が確定したとみなして一時停止を解除
            _rootVFX.enabled = true;
            _rootVFX.pause = false;

            // 原因対策4：再生直後のフレームはパーティクル数が0になるため、生存フラグを立てる
            _isStarting = true;

            // 再生イベントを送信
            _rootVFX.SendEvent(VisualEffectAsset.PlayEventID);
        }
    }

    protected override bool IsAliveInternal()
    {
        Debug.Log("a");

        _currentAliveTime += Time.deltaTime;

        if(_currentAliveTime > _despawnTime)
        {
            return false;
        }

        return true;

        // 原因対策4：再生直後の最初のフレームは、カウントが0でも強制的に生存（true）を返す
        //if (_isStarting)
        //{
        //    _isStarting = false;
        //    return true;
        //}

        //if (_vfxs == null) return false;

        //// 画面内に1つでも生きているパーティクルがあれば「生存」
        //foreach (var vfx in _vfxs)
        //{
        //    if (vfx == null) continue;

        //    if (vfx.aliveParticleCount > 0)
        //    {
        //        return true;
        //    }
        //}

        // すべてのパーティクルが消えたら死亡（false）を返し、外側のシステムに回収させる
        //return false;
    }

    protected override void OnStopInternal()
    {
        if (_rootVFX != null)
        {
            // 新規生成を止め、既存のパーティクルを即座に消去する
            _rootVFX.SendEvent(VisualEffectAsset.StopEventID);
            _rootVFX.Reinit();
        }
        _isStarting = false;
    }

    protected override void ApplyTimeScaleInternal(float scale)
    {
        if (_vfxs == null) return;
        foreach (var vfx in _vfxs)
        {
            if (vfx == null) continue;
            vfx.playRate = scale;
        }
    }

    protected override void ApplyScaleInternal(Vector3 scale)
    {
        transform.localScale = scale;
    }
}
