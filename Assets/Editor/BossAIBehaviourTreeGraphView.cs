using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BossEnemy.AI.Editor.GraphView
{
    public class BossAIBehaviourTreeGraphView : BehaviourTreeGraphView
    {
        public BossAIBehaviourTreeGraphView(EditorWindow editorWindow) : base(editorWindow)
        {
            // 右クリックメニューを追加
            var menuWindowProvider = ScriptableObject.CreateInstance<SearchMenuWindowProvider>();

            menuWindowProvider.Init(this, editorWindow);

            nodeCreationRequest += context =>
            {
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), menuWindowProvider);
            };
        }
    }
        
    public class SearchMenuWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        private BossAIBehaviourTreeGraphView _graphView;
        private EditorWindow _editorWindow;

        /// <summary> 初期化 </summary>
        /// <param name="graphView"></param>
        /// <param name="editorWindow"></param>
        public void Init(BossAIBehaviourTreeGraphView graphView, EditorWindow editorWindow)
        {
            _graphView = graphView;
            _editorWindow = editorWindow;
        }

        /// <summary>  </summary>
        List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>();
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Create Node")));

            // ActionNodeのグループを追加
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Action")) { level = 1 });

            // DecoratorNodeのグループを追加
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Decorator")) { level = 1 });

            // SelectorNodeのグループを追加
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Selector")) { level = 1 });
            entries.Add(new SearchTreeEntry(new GUIContent(nameof(SelectorNode))) { level = 2, userData = typeof(SelectorNode) });

            // SequenceNodeのグループを追加
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Sequence")) { level = 1 });

            return entries;
        }

        bool ISearchWindowProvider.OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            var type = searchTreeEntry.userData as Type;
            var node = Activator.CreateInstance(type) as Node;

            // マウスの位置にノードを追加
            var worldMousePosition = _editorWindow.rootVisualElement.ChangeCoordinatesTo(_editorWindow.rootVisualElement.parent, context.screenMousePosition - _editorWindow.position.position);
            var localMousePosition = _graphView.contentViewContainer.WorldToLocal(worldMousePosition);
            node.SetPosition(new Rect(localMousePosition, new Vector2(100, 100)));

            _graphView.AddElement(node);
            return true;
        }
    }
}
