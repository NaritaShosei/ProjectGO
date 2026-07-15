using UnityEngine;

public class ResultUIManager : MonoBehaviour
{
    public static ResultPanelView GetOrCreateView(ResultPanelView configuredView)
    {
        return configuredView != null ? configuredView : ResultPanelView.CreateRuntime();
    }
}
