using Boss.AI.BehaviourTree;
using Boss.Character;
using Boss.Interface;
using System;
using UnityEngine;

[Serializable]
public abstract class BossCharacterAIBehaviourTreeNode<T> : 
    TreeNode, IBossAIBehaviourTreeNode, IPrototype<T> where T : TreeNode
{
    public virtual void Init(IBossCharacterEntity bossCharacterEntity, NodeRunningConditionNotifier nodeRunningEndNotifier)
    {
        Init(nodeRunningEndNotifier);

        _bossCharacterEntity = bossCharacterEntity;

        // 子ノードを初期化
        if (_childrenNode != null && _childrenNode.Length > 0)
        {
            foreach (var child in _childrenNode)
            {
                InitChildren(child, nodeRunningEndNotifier);
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        _bossCharacterEntity = null;

        // 子ノードを初期化
        if (_childrenNode != null && _childrenNode.Length > 0)
        {
            foreach (var child in _childrenNode)
            {
                if (child == null) continue;

                child.Dispose();
            }
        }
    }

    public virtual T Clone()
    {
        Debug.LogError($"{ typeof(T) } クラス内でCloneメソッドが正しく実装されていません");
        return default;
    }

    protected IBossCharacterEntity _bossCharacterEntity = null;

    /// <summary> 子ノードを初期化 </summary>
    protected virtual void InitChildren(TreeNode treeNode, NodeRunningConditionNotifier nodeRunningEndNotifier)
    {
        if (treeNode == null)
        {
            Debug.LogError("BehaviourTreeにNullの子ノードが設定されています。");
            return;
        }

        if (treeNode is IBossAIBehaviourTreeNode bossCharacterBehaviourTreeNode)
        {
            bossCharacterBehaviourTreeNode.Init(_bossCharacterEntity, nodeRunningEndNotifier);
            return;
        }

        treeNode.Init(nodeRunningEndNotifier);
    }
}
