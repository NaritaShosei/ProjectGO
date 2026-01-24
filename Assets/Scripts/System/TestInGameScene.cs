using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks; 

public class TestInGameUIManager : MonoBehaviour
{
    [SerializeField]
    private Button _transitionToResult;
    private SceneTransitionManager _sceneTransitionManager;
    private void Start()
    {
        if (!ServiceLocator.TryGet(out _sceneTransitionManager))
        {
            Debug.LogError("SceneTransitionManager is not registered in ServiceLocator.", this);
            return;
        }
        _transitionToResult.onClick.AddListener(() =>
        {
            HandleTransitionToResult().Forget();
        });
    }

    private void OnDestroy()
    {
        _transitionToResult.onClick.RemoveAllListeners();
    }

    private async UniTask HandleTransitionToResult()
    {
        await _sceneTransitionManager.TransitionToResult();
    }
}
