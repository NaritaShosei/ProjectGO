using BossEnemy.AI.BehaviourTree;
using BossEnemy.Character;
using System;
using UnityEngine;

[Serializable]
public abstract class BossCharacterBehaviourTreeNode : TreeNode
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

    protected IBossCharacterEntity _bossCharacterEntity = null;

    /// <summary> 子ノードを初期化 </summary>
    protected virtual void InitChildren(TreeNode treeNode, NodeRunningConditionNotifier nodeRunningEndNotifier)
    {
        if (treeNode == null)
        {
            Debug.LogError("BehaviourTreeにNullの子ノードが設定されています。");
            return;
        }

        if (treeNode is BossCharacterBehaviourTreeNode bossCharacterBehaviourTreeNode)
        {
            bossCharacterBehaviourTreeNode.Init(_bossCharacterEntity, nodeRunningEndNotifier);
            return;
        }

        treeNode.Init(nodeRunningEndNotifier);
    }
}
