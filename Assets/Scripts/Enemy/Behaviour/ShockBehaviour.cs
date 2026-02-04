using UnityEngine;

// TODO: Contextへの依存を削除
public class ShockBehaviour : IEnemyBehaviour
{
    public void Init(
        Enemy owner,
        EnemyData data,
        Transform player,
        EnemyContext context,
        EnemyStateManager state
     )
    {
        _self = owner.transform;
        _player = player;
        _data = data;
        _context = context;
        _state = state;

        _material = _self.gameObject.GetComponent<Renderer>().material;

    }


    public void Tick(float deltaTime)
    {
        if (_player == null) { return; }

        // 攻撃の条件に満たしていなかったら早期リターン
        if (!_state.IsShock) { return; }

        // 感電開始
        if (!_isShocking)
        {
            StartShock();
        }
        
        // 感電継続時間内ならreturn
        if (_remainTime > 0)
        {
            _remainTime -= deltaTime;
            Shock();
        }
        else
        {
            // 感電終了時
            EndShock();
        }
    }

    // TODO: スタン時の挙動を実装
    private void StartShock()
    {
        _isShocking = true;
        _remainTime = 5f;


        // TODO Debug 問題なければ消す
        Debug.Log("感電開始");
    }

    private void Shock()
    {
        if(_material == null) {return; }

        // 0～cycleの範囲の値が得られる
        var repeatValue = Mathf.Repeat(_remainTime, _cycle);

        // sin波でフェードさせる
        float alpha = (Mathf.Sin((repeatValue / _cycle) * Mathf.PI * 2f) + 1f) * 0.5f;

        // 内部時刻_remainTimeにおける明滅状態を反映
        // マテリアル色のアルファ値を変更している
        var color = _material.color;
        color.a = alpha;
        _material.color = color;
    }

    // スタン終了時の挙動
    private void EndShock()
    {
        // 表示を戻す。つまり透明度を1に戻す。
        var color = _material.color;
        color.a = 1;
        _material.color = color;

        // ShockステートからIdleへ戻す。
        _state.ChangeState(EnemyState.Idle);
        _isShocking=false;

        // TODO Debug 問題なければ消す
        Debug.Log("感電終了");
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;
    private EnemyStateManager _state;

    // 感電状態用の変数
    private bool _isShocking = false;
    private float _remainTime = -1f;     // 5秒間停止

    // 点滅用の変数
    private float _cycle = 1f;      // 点滅感覚の秒数
    private Material _material = null;
}
