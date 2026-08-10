using UnityEditor;
using BossEnemy.AI.Editor.GraphView;

namespace BossEnemy.AI.Editor.Window
{
    public class BossAIBehaviourGraphEditorWindow : EditorWindow
    {
        [MenuItem("Window/GraphView/BossAIGraphEditorWindow")]
        public static void Open()
        {
            GetWindow<BossAIBehaviourGraphEditorWindow>(ObjectNames.NicifyVariableName(nameof(BossAIBehaviourGraphEditorWindow)));
        }

        void OnEnable()
        {
            var graphView = new BossAIBehaviourTreeGraphView(this);
            rootVisualElement.Add(graphView);
        }
    }
}
