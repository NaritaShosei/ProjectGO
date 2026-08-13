using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

public class SceneTransitionManager : MonoBehaviour
{
    private const string SystemSceneName = "SystemScene";

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
        await LoadSceneAsync("TitleScene");
    }

    public async UniTask TransitionToResult()
    {
        await LoadSceneAsync("ResultScene");
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
        if (ServiceLocator.TryGet(out SceneTransitionManager current) && ReferenceEquals(current, this))
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

        var destinationScene = SceneManager.GetSceneByName(sceneName);
        if (!destinationScene.isLoaded)
        {
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            destinationScene = SceneManager.GetSceneByName(sceneName);
        }

        SceneManager.SetActiveScene(destinationScene);

        // SystemSceneと遷移先を残し、それ以外のゲームシーンをアンロードする。
        for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var loadedScene = SceneManager.GetSceneAt(i);
            if (loadedScene.name == SystemSceneName || loadedScene == destinationScene)
            {
                continue;
            }

            await SceneManager.UnloadSceneAsync(loadedScene);
        }
    }
}
