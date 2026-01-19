using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

public class SceneTransitionManager : MonoBehaviour
{
    /// <summary>
    /// 任意シーンへ遷移する
    /// </summary>
    /// <param name="sceneName">遷移先のシーン</param>
    public async UniTask TransitionToScene(string sceneName)
    {
        await LoadSceneAsync(sceneName);
    }

    /// <summary>
    /// タイトルへ遷移する
    /// </summary>
    public async UniTask TransitionToTitle()
    {
        await LoadSceneAsync("Title");
    }

    private void Awake()
    {
        if (!ServiceLocator.IsRegistered<SceneTransitionManager>())
        {
            ServiceLocator.Register(this);
        }
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<SceneTransitionManager>())
        {
            ServiceLocator.Unregister<SceneTransitionManager>();
        }
    }

    /// <summary>
    /// シーン遷移を非同期で行う汎用コルーチン
    /// </summary>
    /// <param name="sceneName">遷移先</param>
    private async UniTask LoadSceneAsync(string sceneName)
    {
        Debug.Log($"{sceneName}：へ遷移する");
        await SceneManager.LoadSceneAsync(sceneName);
    }
}
