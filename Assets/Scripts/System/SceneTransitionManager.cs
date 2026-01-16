using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    /// <summary>
    /// 任意シーンへ遷移する
    /// </summary>
    /// <param name="sceneName">遷移先のシーン</param>
    public void TransitionToScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    /// <summary>
    /// タイトルへ遷移する
    /// </summary>
    public void TransitionToTitle()
    {
        LoadSceneAsync("Title");
    }


    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    /// <summary>
    /// シーン遷移を非同期で行う汎用コルーチン
    /// </summary>
    /// <param name="sceneName">遷移先</param>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        Debug.Log($"{sceneName}：へ遷移する");

        var asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncOperation.isDone)
        {
            yield return null;
        }
    }
}
