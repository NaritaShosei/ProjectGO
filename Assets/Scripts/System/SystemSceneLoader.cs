using UnityEngine;
using UnityEngine.SceneManagement;

public class SystemSceneLoader
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void LoadSystemScene()
    {
        SceneManager.LoadScene("SystemScene", LoadSceneMode.Additive);
    }
}
