using BossEnemy.Character;
using System;
using UniRx;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BossEnemy.AI
{
    /// <summary> Nodeへの遷移結果 </summary>
    public enum NodeCondition
    {
        Success,
        Failure,
        Running
    }

    #region 実行中のノードの実行状況をControllerに通知するクラス
    /// <summary> 実行中のノードの実行状況をControllerに通知するクラス </summary>
    public class RunningConditionNotifier
    {
        public event Action OnRunningEnd;

        public void HandleRunningEnd()
        {
            OnRunningEnd?.Invoke();
        }
    }
    #endregion

    #region TreeのNode遷移を行い操作するクラス

    /// <summary> BehaviorTreeの操作クラス </summary>
    public class BehaviourController
    {
        public BehaviourController(
            BossCharacterEntity bossCharacterEntity, 
            ITreeNode entryNode)
        {
            _entryNode = entryNode;
        }

        /// <summary> 毎フレーム実行する処理 </summary>
        public void OnUpdate()
        {
            if (_runningNode == null) return;

            _runningNode.OnUpdate();
        }

        /// <summary> BehaviourTreeを探索し次の実行ノードを決める処理 </summary>
        public void SearchNextRunningNode()
        {
            if(_entryNode == null) return;

            NodeCondition runningCondition = NodeCondition.Success;

            int count = 0;
            while (runningCondition != NodeCondition.Running)
            {
                runningCondition = _runningNode.TryEntryNextNode(_runningNode);
                count++;

                if (runningCondition == NodeCondition.Failure)
                {
                    Debug.LogError("行動の選択を失敗しました");
                    return;
                }
            }

            ChangeRunningNode(_runningNode);
        }

        /// <summary> 現在の行動を強制的に停止させる処理 </summary>
        public void StopRunning()
        {
            if (_entryNode == null) return;

            if (_runningNode != null)
                _runningNode.OnExit();
        }

        /// <summary> 現在実行中のNode </summary>
        private ITreeNode _runningNode = null;

        /// <summary> 操作するBossの実体クラス </summary>
        private BossCharacterEntity _bossCharacterEntity = null;

        /// <summary> 探索開始地点 </summary>
        private readonly ITreeNode _entryNode = null;

        /// <summary> 現在のNodeの実行状況通知クラス </summary>
        private readonly RunningConditionNotifier _nodeRunningEndNotifier = new();

        /// <summary> 現在のNodeを変更する </summary>
        /// <param name="nextAction"> 次のNode </param>
        private void ChangeRunningNode(ITreeNode nextAction)
        {
            if(_runningNode != null) _runningNode.OnExit();

            if (nextAction == null) return;
            if (!nextAction.IsInit) nextAction.Init(_bossCharacterEntity, _nodeRunningEndNotifier);

            _runningNode = nextAction;
            _runningNode.OnEnter();
        }
    }
    #endregion

    #region BehaviourTreeNodeのInterface
    /// <summary> TreeNodeのInterface </summary>
    public interface ITreeNode
    {
        /// <summary> 初期化済み判定フラグ </summary>
        public bool IsInit { get; }

        /// <summary> BehaviourTreeをSetする </summary>
        void Init(BossCharacterEntity bossCharacterEntity, RunningConditionNotifier nodeRunningEndNotifier);

        /// <summary> このNodeへの遷移条件を確認して結果を返す </summary>
        NodeCondition TryEntry();

        /// <summary> 子ノードから遷移可能なノードを選出して渡す </summary>
        /// <param name="nextNode"> 次のNode </param>
        /// <returns> 
        /// 次のNodeへの遷移結果フラグ
        /// このフラグがFalseなら現在のNodeをゴールとする
        /// </returns>
        NodeCondition TryEntryNextNode(ITreeNode nextNode);

        /// <summary> このNodeへの遷移が成功した際の処理 </summary>
        void OnEnter();

        /// <summary> このNodeの実行中の処理 </summary>
        void OnUpdate();

        /// <summary> このNodeを離れる際の処理 </summary>
        void OnExit();
    }
    #endregion

    #region BehaviourTreeNodeの基底Class
    /// <summary> BehaviorTreeのNodeの基底クラス </summary>
    public abstract class TreeNodeBase : Node, ITreeNode
    {
        public bool IsInit => _isInit;

        public virtual void Init(
            BossCharacterEntity bossCharacterEntity,
            RunningConditionNotifier nodeRunningEndNotifier)
        {
            _isInit = true;
            _nodeRunningEndNotifier = nodeRunningEndNotifier;
        }

        public abstract NodeCondition TryEntry();
        public abstract NodeCondition TryEntryNextNode(ITreeNode nextNode);
        public virtual void OnEnter() { return; }
        public virtual void OnUpdate() { return; }
        public virtual void OnExit() { return; }

        // 操作するボスのEntity
        protected BossCharacterEntity _bossCharacterEntity;

        private RunningConditionNotifier _nodeRunningEndNotifier = null;

        private bool _isInit = false;

        protected void RunningEnd() => _nodeRunningEndNotifier.HandleRunningEnd();
    }
    #endregion

    #region BehaviorTreeを構築するTreeGraph生成用のEditor拡張
    /// <summary> BehaviorTreeを構築するGraphView </summary>
    public abstract class BehaviourTreeGraphView : GraphView
    {
        public BehaviourTreeGraphView(EditorWindow editorWindow)
        {
            // 親のサイズに合わせてGraphViewのサイズを設定
            this.StretchToParentSize();

            // MMBスクロールでズームインアウトができるように
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            // MMBドラッグで描画範囲を動かせるように
            this.AddManipulator(new ContentDragger());
            // LMBドラッグで選択した要素を動かせるように
            this.AddManipulator(new SelectionDragger());
            // LMBドラッグで範囲選択ができるように
            this.AddManipulator(new RectangleSelector());
        }

        protected BehaviourTreeGraphPresenter _behaviourTreeGraphPresenter;
    }

    /// <summary>  </summary>
    public class BehaviourTreeGraphPresenter
    {

    }
    #endregion
}
