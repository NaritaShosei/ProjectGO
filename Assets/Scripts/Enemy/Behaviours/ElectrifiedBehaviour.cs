using UnityEngine;

// TODO: Contextへの依存を削除
public class ElectrifiedBehaviour : IEnemyBehaviour
{
    public void Init(
        Enemy owner,
        EnemyData data,
        Transform player,
        EnemyContext context,
        EnemyStateContext state
     )
    {
        _self = owner.transform;
        _player = player;
        _data = data;
        _context = context;
        _state = state;

        _material = _self.gameObject.GetComponent<Renderer>().material;

        _isElectrifiedShocking = false;
    }


    public void Tick(float deltaTime)
    {
        if (_player == null) { return; }

        // 攻撃の条件に満たしていなかったら早期リターン
        if (!_state.IsElectrified) { return; }

        // 感電開始
        if (!_isElectrifiedShocking)
        {
            StartElectrifiedShock();
        }

        // 感電継続時間内ならreturn
        if (_durationTime > 0)
        {
            _durationTime -= deltaTime;
            ElectrifiedShock();
        }
        else
        {
            // 感電終了時
            EndElectrifiedShock();
        }
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
    private EnemyStateContext _state;

    // 感電状態を保持する変数
    private bool _isElectrifiedShocking = false;
    private float _durationTime;

    // 点滅表示に使用する変数
    private float _cycle = 1f;      // 点滅感覚の秒数
    private Material _material = null;


    // 感電開始時のみ
    private void StartElectrifiedShock()
    {
        _isElectrifiedShocking = true;

        // 継続時間はEnemyStateContextを参照して更新
        _durationTime = _state.DurationElectrifiedTime;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

        // TODO Debug 問題なければ消す
        Debug.Log("感電開始");
#endif

    }

    // 感電中
    private void ElectrifiedShock()
    {
        if (_material == null) { return; }

        // 0～cycleの範囲の値が得られる
        var repeatValue = Mathf.Repeat(_durationTime, _cycle);

        // sin波でフェードさせる
        float alpha = (Mathf.Sin((repeatValue / _cycle) * Mathf.PI * 2f) + 1f) * 0.5f;

        // 内部時刻_remainTimeにおける明滅状態を反映
        // マテリアル色のアルファ値を変更している
        var color = _material.color;
        color.a = alpha;
        _material.color = color;
    }

    // 感電終了時
    private void EndElectrifiedShock()
    {
        if (_material != null) 
        {
            // 表示を戻す。つまり透明度を1に戻す。
            var color = _material.color;
            color.a = 1;
            _material.color = color;
        }

        // ShockステートからIdleへ戻す。
        _state.ChangeState(EnemyState.Idle);
        _isElectrifiedShocking = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

        // TODO Debug 問題なければ消す
        Debug.Log("感電終了");
#endif

        // durationTimeを初期化しておく。
        _state.SetElectrifiedTime(0);
    }

    public int Priority { get; }

    public bool CanEnter() { return true; }
    public bool CanContinue() { return true; }

    public void OnEnter() { }
    public void OnExit() { }
}
