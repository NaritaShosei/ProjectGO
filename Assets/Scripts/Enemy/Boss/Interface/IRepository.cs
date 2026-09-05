using BossEnemy.AI.BehaviourTree;
using BossEnemy.Attack;
using BossEnemy.Character;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace BossEnemy.Interface
{
    public interface IBossEnemyAttackDataRepository : ICSVDataLoadRepository
    {
        public void Init();

        public Attack.AttackData GetData(int id);
    }

    public interface IBossEnemyAttackSelectionPoolRepository : ICSVDataLoadRepository
    {
        public void Init();

        public AttackSelectionPool GetSelectionPool(int id);
    }

    public interface IBossCharacterEntityRepository : ICSVDataLoadRepository
    {
        public void Init();

        public BossCharacterEntity GetEntity(int id);
    }

    public interface IBossAIBehaviourTreeNodeRepository
    {
        public bool TryGetEntryNode(int id, out EntryNode entryNode);

        /// <summary> 新しいBehaviourTreeのEntryNodeを保存する </summary>
        public void SaveEntryNode(EntryNode newEntryNode);

        public void AddTreeNode(EntryNode entryNode, TreeNode node);

        public void RemoveTreeNode(EntryNode entryNode, TreeNode node);

        /// <summary> EntryNodeに接続されているTreeNode群を同期する </summary>
        public void SyncTreeNodes(EntryNode entryNode, IEnumerable<TreeNode> connectedNodes);
    }

    public interface ICSVDataLoadRepository
    {
        public const string CSV_DATA_SEARCH_END_KEY = "END";

        public string CSVDataSearchStartKey { get; }
    }
}
