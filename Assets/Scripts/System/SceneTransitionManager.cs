using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

public class SceneTransitionManager : MonoBehaviour
{
    private const string SystemSceneName = "SystemScene";

    //読み込み時最低でも5秒間ロード画面を見せる
    private const float MinimumLoadingDuration = 5f;
    private const float ProgressSpeed = 1f;

    /// <summary>
    /// ロード画面を含むシーン遷移処理中かどうか
    /// </summary>
    public bool IsTransitioning => _isTransitioning;

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

    [SerializeField] private LoadingScreenView _loadingScreen;
    private bool _isTransitioning;

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
        if (_isTransitioning)
        {
            Debug.LogWarning("シーン遷移中に再度遷移が要求されました。");
            return;
        }

        if (_loadingScreen == null)
        {
            Debug.LogError("LoadingScreenViewが設定されていません。", this);
            return;
        }

        Scene managerScene = gameObject.scene;
        Scene loadingScreenScene = _loadingScreen.gameObject.scene;

        Debug.Log(
            $"Manager: {managerScene.name}, " +
            $"LoadingScreen: {loadingScreenScene.name}");


        _isTransitioning = true;

        ServiceLocator.TryGet(out InputHandler inputHandler);

        bool loadingScreenShown = false;
        bool gameTimePaused = false;
        float previousTimeScale = Time.timeScale;

        try
        {
            Debug.Log($"{sceneName}へ遷移する");

            inputHandler?.EnableInput(false);

            // 遷移先シーンがロードされても、ロード画面を閉じるまでは
            // 敵・物理・Time.deltaTimeを使うタイマーを進行させない。
            Time.timeScale = 0f;
            gameTimePaused = true;

            float displayedProgress = 0f;

            _loadingScreen.SetProgress(displayedProgress);
            await _loadingScreen.ShowAsync();
            loadingScreenShown = true;

            var destinationScene =
                SceneManager.GetSceneByName(sceneName);

            if (!destinationScene.isLoaded)
            {
                AsyncOperation operation =
                    SceneManager.LoadSceneAsync(
                        sceneName,
                        LoadSceneMode.Additive);

                if (operation == null)
                {
                    throw new System.InvalidOperationException(
                        $"シーンのロードを開始できませんでした: {sceneName}");
                }

                float loadingStartedAt = Time.unscaledTime;


                while (!operation.isDone ||
                       Time.unscaledTime - loadingStartedAt < MinimumLoadingDuration)
                {
                    float actualProgress = operation.isDone
                        ? 1f
                        : Mathf.Clamp01(operation.progress / 0.9f);

                    float timeProgress = Mathf.Clamp01(
                        (Time.unscaledTime - loadingStartedAt) /
                        MinimumLoadingDuration);

                    // 実ロードと最低表示時間の遅い方に合わせる
                    float targetProgress =
                        Mathf.Min(actualProgress, timeProgress);

                    displayedProgress = Mathf.MoveTowards(
                        displayedProgress,
                        targetProgress,
                        ProgressSpeed * Time.unscaledDeltaTime);

                    _loadingScreen.SetProgress(displayedProgress);

                    await UniTask.Yield();
                }

                _loadingScreen.SetProgress(1f);
                await UniTask.NextFrame();

                destinationScene =
                    SceneManager.GetSceneByName(sceneName);
            }

            if (!destinationScene.IsValid() ||
                !destinationScene.isLoaded)
            {
                throw new System.InvalidOperationException(
                    $"ロード後のシーンを取得できませんでした: {sceneName}");
            }

            SceneManager.SetActiveScene(destinationScene);

            // SystemSceneと遷移先以外をアンロード
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);

                if (loadedScene == destinationScene ||
                    loadedScene == managerScene ||
                    loadedScene == loadingScreenScene)
                {
                    continue;
                }

                Debug.Log($"アンロード: {loadedScene.name}");
                await SceneManager.UnloadSceneAsync(loadedScene);
            }

            // 実際のロード完了後、表示が100%に追いつくまで待つ
            while (displayedProgress < 1f)
            {
                displayedProgress = Mathf.MoveTowards(
                    displayedProgress,
                    1f,
                    ProgressSpeed * Time.unscaledDeltaTime);

                _loadingScreen.SetProgress(displayedProgress);

                await UniTask.Yield();
            }

            await UniTask.NextFrame();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            try
            {
                if (loadingScreenShown && _loadingScreen != null)
                {
                    await _loadingScreen.HideAsync();
                }
            }
            catch (MissingReferenceException exception)
            {
                Debug.LogWarning(
                    $"ロード画面がフェード中に破棄されました: " +
                    $"{exception.Message}");
            }
            finally
            {
                if (gameTimePaused)
                {
                    Time.timeScale = previousTimeScale;
                }

                inputHandler?.EnableInput(true);
                _isTransitioning = false;
            }
        }
    }
}
