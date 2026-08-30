using UnityEngine;
using UnityEngine.SceneManagement;

public class SystemSceneLoader
{
    private const string SystemSceneName = "SystemScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void LoadSystemScene()
    {
        Scene systemScene =
            SceneManager.GetSceneByName(SystemSceneName);

        if (!systemScene.isLoaded)
        {
            SceneManager.LoadScene(
                SystemSceneName,
                LoadSceneMode.Additive);
        }
    }
}
