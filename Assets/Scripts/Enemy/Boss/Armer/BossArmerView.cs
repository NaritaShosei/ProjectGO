using Cysharp.Threading.Tasks;
using UnityEngine;


/// <summary> ボスの装備するアーマー </summary>
public class BossArmerView : MonoBehaviour
{
    public void Init(BossEnemyView bossEnemyView)
    {
        _bossEnemyView = bossEnemyView;
    }

    /// <summary> アーマー破壊時の処理 </summary>
    public async UniTask ArmerBreak()
    {
        await UniTask.CompletedTask;
    }

    private BossEnemyView _bossEnemyView = null;
}
