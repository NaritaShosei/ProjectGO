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
        // TODO: スタン継続時間内ならreturn
        // TODO: スタン終了アクション
    }

    private Transform _self;
    private Transform _player;
    private EnemyData _data;
    private EnemyContext _context;

    // TODO: 残りスタン秒数を保持する変数を用意
}
