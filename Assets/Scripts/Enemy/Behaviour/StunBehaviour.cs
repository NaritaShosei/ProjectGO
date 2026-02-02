using UnityEngine;

public class StunBehaviour : IEnemyBehaviour
{
    public void Init(
         Enemy owner,
         EnemyData data,
         Transform player,
         EnemyContext context
     )
    {
        _self = owner.transform;
        _player = player;
        _data = data;
        _context = context;
    }


    public void Tick(float deltaTime)
    {
        // TODO: スタン開始の検知
        // TODO: スタン時の挙動
        Stun();
        // TODO: スタン継続時間内ならreturn
        // TODO: スタン終了アクション
        OnStunEnd();
    }

    // TODO: スタン時の挙動を実装
    private void Stun()
    {
        // TODO: スタン時の見た目はひとまず点滅にしておく。
        // TODO: マテリアルの透明度を変えることで実装したい
    }

    // TODO: スタン終了時の挙動を実装
    private void OnStunEnd()
    {
        // TODO: 表示を戻す。つまり透明度を1に戻す。
        // TODO: 明示的にマテリアルのインスタンスを破棄する。
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;

    // TODO: 残りスタン秒数を保持する変数を用意

    // TODO: ゲームオブジェクトのマテリアルを保持する変数を用意
}
