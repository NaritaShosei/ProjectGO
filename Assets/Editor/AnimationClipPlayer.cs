using UnityEngine;
using UnityEditor;

public class AnimationClipPlayer : EditorWindow
{
    private AnimationClip clip;
    private GameObject previewObject;
    private float currentTime = 0f;
    private bool isPlaying = false;
    private double lastEditorTime;

    [MenuItem("Tools/Animation Clip Player")]
    static void Open()
    {
        GetWindow<AnimationClipPlayer>("Animation Player");
    }

    void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        lastEditorTime = EditorApplication.timeSinceStartup;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    void OnEditorUpdate()
    {
        if (isPlaying && clip != null && previewObject != null)
        {
            double deltaTime = EditorApplication.timeSinceStartup - lastEditorTime;
            currentTime += (float)deltaTime;

            // ループ処理
            if (currentTime > clip.length)
            {
                currentTime = currentTime % clip.length;
            }

            // アニメーションをサンプリング
            clip.SampleAnimation(previewObject, currentTime);

            // SceneViewを更新
            SceneView.RepaintAll();
            Repaint();
        }

        lastEditorTime = EditorApplication.timeSinceStartup;
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Animation Clip Player", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // AnimationClip選択
        clip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", clip, typeof(AnimationClip), false);

        // プレビュー対象のGameObject選択
        previewObject = (GameObject)EditorGUILayout.ObjectField("Preview Object", previewObject, typeof(GameObject), true);

        if (clip == null)
        {
            EditorGUILayout.HelpBox("AnimationClipを設定してください", MessageType.Info);
            return;
        }

        if (previewObject == null)
        {
            EditorGUILayout.HelpBox("プレビュー対象のGameObjectを設定してください（Hierarchyから選択）", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();

        // クリップ情報表示
        EditorGUILayout.LabelField("Clip Info", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Length: {clip.length:F3} sec");
        EditorGUILayout.LabelField($"Frame Rate: {clip.frameRate} fps");
        EditorGUILayout.LabelField($"Total Frames: {Mathf.FloorToInt(clip.length * clip.frameRate)}");

        EditorGUILayout.Space();

        // 再生コントロール
        EditorGUILayout.LabelField("Playback Controls", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // 再生/停止ボタン
        if (GUILayout.Button(isPlaying ? "Pause" : "Play", GUILayout.Height(30)))
        {
            isPlaying = !isPlaying;
            if (isPlaying)
            {
                lastEditorTime = EditorApplication.timeSinceStartup;
            }
        }

        // 停止ボタン
        if (GUILayout.Button("Stop", GUILayout.Height(30)))
        {
            isPlaying = false;
            currentTime = 0f;
            if (previewObject != null)
            {
                clip.SampleAnimation(previewObject, currentTime);
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // タイムスライダー
        EditorGUI.BeginChangeCheck();
        float newTime = EditorGUILayout.Slider("Time", currentTime, 0f, clip.length);
        if (EditorGUI.EndChangeCheck())
        {
            currentTime = newTime;
            if (previewObject != null)
            {
                clip.SampleAnimation(previewObject, currentTime);
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.LabelField($"Current Time: {currentTime:F3} sec / {clip.length:F3} sec");

        // フレーム単位での移動
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Frame Control", EditorStyles.boldLabel);

        int currentFrame = Mathf.FloorToInt(currentTime * clip.frameRate);
        int totalFrames = Mathf.FloorToInt(clip.length * clip.frameRate);

        EditorGUI.BeginChangeCheck();
        int newFrame = EditorGUILayout.IntSlider("Frame", currentFrame, 0, totalFrames);
        if (EditorGUI.EndChangeCheck())
        {
            currentTime = newFrame / clip.frameRate;
            if (previewObject != null)
            {
                clip.SampleAnimation(previewObject, currentTime);
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Previous Frame"))
        {
            currentFrame = Mathf.Max(0, currentFrame - 1);
            currentTime = currentFrame / clip.frameRate;
            if (previewObject != null)
            {
                clip.SampleAnimation(previewObject, currentTime);
                SceneView.RepaintAll();
            }
        }

        if (GUILayout.Button("Next Frame"))
        {
            currentFrame = Mathf.Min(totalFrames, currentFrame + 1);
            currentTime = currentFrame / clip.frameRate;
            if (previewObject != null)
            {
                clip.SampleAnimation(previewObject, currentTime);
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();

        // AnimationEventの表示
        if (clip.events.Length > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Events", EditorStyles.boldLabel);

            foreach (var evt in clip.events)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{evt.functionName}: {evt.time:F3} sec");
                if (GUILayout.Button("Jump", GUILayout.Width(50)))
                {
                    currentTime = evt.time;
                    if (previewObject != null)
                    {
                        clip.SampleAnimation(previewObject, currentTime);
                        SceneView.RepaintAll();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
