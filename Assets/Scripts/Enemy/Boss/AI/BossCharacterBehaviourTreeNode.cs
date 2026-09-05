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

        if(_childrenNode != null && _childrenNode.Length > 0)
        {
            foreach (var child in _childrenNode)
            {
                InitChildren(child);
            }
        }
    }

    protected IBossCharacterEntity _bossCharacterEntity = null;

    protected virtual void InitChildren(TreeNode treeNode)
    {
        if (treeNode.IsInit) return;

        if (treeNode is BossCharacterBehaviourTreeNode bossCharacterBehaviourTreeNode)
        {
            bossCharacterBehaviourTreeNode.Init(_bossCharacterEntity, _nodeRunningConditionNotifier);
            return;
        }

        treeNode.Init(_nodeRunningConditionNotifier);
    }
}
