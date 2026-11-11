using Boss.AI.BehaviourTree;
using Boss.Attack;
using Boss.Character;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace Boss.Interface
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
        public void Init(ICanMoveAreaChecker canMoveAreaChecker);

        public BossCharacterEntity GetEntity(int id);

        public void ReleaseEntity(int id, BossCharacterEntity bossCharacterEntity);
    }

    public interface IBossAIBehaviourTreeNodeRepository
    {
        /// <summary> 指定したIDのビヘイビアツリーのエントリーノード取得 </summary>
        public bool TryGetEntryNode(int id, out EntryNode entryNode);

        /// <summary> 使用したノードの返却 </summary>
        public void ReleaseNode(EntryNode entryNode);

        // -------------- Editor限定機能 -----------------------------------------------
#if UNITY_EDITOR
        /// <summary> 新しいBehaviourTreeのEntryNodeを保存する </summary>
        public void SaveEntryNode(EntryNode newEntryNode);

        /// <summary> 新たなノードを追加する </summary>
        public void AddTreeNode(EntryNode entryNode, TreeNode node);

        /// <summary> 不要なノードを削除する </summary>
        public void RemoveTreeNode(EntryNode entryNode, TreeNode node);

        /// <summary> EntryNodeに接続されているTreeNode群を同期する </summary>
        public void SyncTreeNodes(EntryNode entryNode, IEnumerable<TreeNode> connectedNodes);
#endif
    }

    public interface ICSVDataLoadRepository
    {
        public const string CSV_DATA_SEARCH_END_KEY = "END";

        public string CSVDataSearchStartKey { get; }
    }
}
