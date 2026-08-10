using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BossEnemy.AI.Editor.Window
{
    public class BossAIBehaviourTreeGraphView : BehaviourTreeGraphView
    {
        public BossAIBehaviourTreeGraphView(EditorWindow editorWindow) : base(editorWindow)
        {

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

            // 各Nodeのグループを追加
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Action")) { level = 1 });
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Decorator")) { level = 2 });
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Selector")) { level = 3 });
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Sequence")) { level = 4 });

            // グループの下に各ノードを作るためのメニューを追加


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
