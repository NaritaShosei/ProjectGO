using UnityEngine;
using System.Collections.Generic;

// TODO: ひとまずEnemyには実装せず
// TODO: BehaviourRunner改築後に対応する
public sealed class EnemyConditionController
{
    public EnemyConditionController(IEnemy enemy)
    {
        _enemy = enemy;
    }

    public void Tick(float deltaTime)
    {
        foreach (var item in _active) 
        {
            item.Tick(_enemy, deltaTime);
            // 終了していたら終了リストに登録
            if(item.IsFinished) _finished.Add(item);
        }

        // 終了しているものを_queueから削除
        foreach(var item in _finished)
        {
            item.OnExit(_enemy);
            _active.Remove(item);
        }

        // finishを初期化
        _finished.Clear();
    }

    /// <summary>
    /// この実装はApplyされたときにConditionがnewで追加されていく方式
    /// 無双のことを考えるとnewするのは避けたいので、今後対策を追加する予定
    /// </summary>
    /// <param name="condition"></param>
    public void ApplyCondition(IEnemyCondition condition)
    {
        if (condition.IsFinished) return;
        _active.Add(condition);
        condition.OnEnter(_enemy);
    }

    // TODO: Conditionが同じならばはじくために、同じものか調べるメソッド
    // TODO: ConditionでActionを阻害させるメソッド

    private readonly IEnemy _enemy;
    private readonly List<IEnemyCondition> _active = new();
    private readonly List<IEnemyCondition> _finished = new();
}
