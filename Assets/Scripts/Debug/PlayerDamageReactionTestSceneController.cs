using System.Collections;
using UnityEngine;

/// <summary>
/// プレイヤーのダメージリアクションだけを確認するテストシーン用Controller。
/// 敵・GameManager・SequenceManagerは使用しない。
/// </summary>
public sealed class PlayerDamageReactionTestSceneController : MonoBehaviour
{
    [SerializeField] private Player _playerPrefab;

    private Player _player;
    private Transform _cameraTransform;
    private float _nextReactionTime;
    private string _status = "Initializing...";
    private GUIStyle _panelStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _labelStyle;

    private IEnumerator Start()
    {
        CreateEnvironment();

        InputHandler input;
        while (!ServiceLocator.TryGet(out input))
            yield return null;

        if (_playerPrefab == null)
        {
            _status = "Player prefab is missing.";
            yield break;
        }

        _player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
        _player.name = "DamageReactionTestPlayer";

        SkillManager skillManager = new GameObject("TestSkillManager")
            .AddComponent<SkillManager>();
        _player.Init(skillManager, input);

        _status = "Ready";
    }

    private void LateUpdate()
    {
        if (_player == null || _cameraTransform == null) return;

        Vector3 target = _player.transform.position + Vector3.up;
        _cameraTransform.position = target + new Vector3(0f, 3.5f, -7f);
        _cameraTransform.LookAt(target);
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUILayout.BeginArea(
            new Rect(20f, 20f, 300f, 285f),
            "Damage Reaction Test",
            _panelStyle);

        GUILayout.Space(8f);
        GUILayout.Label(_status, _labelStyle);
        GUILayout.Space(10f);

        bool canReact = _player != null && Time.unscaledTime >= _nextReactionTime;
        GUI.enabled = canReact;
        DrawReactionButton("Small / 小", DamageReactionType.Small);
        DrawReactionButton("Medium / 中", DamageReactionType.Medium);
        DrawReactionButton("Large / 大", DamageReactionType.Large);
        GUI.enabled = true;

        GUILayout.Space(8f);
        GUILayout.Label(
            "WASD: Move\nButtons call TakeDamage with zero damage.",
            _labelStyle);
        GUILayout.EndArea();
    }

    private void DrawReactionButton(string label, DamageReactionType reactionType)
    {
        if (!GUILayout.Button(label, _buttonStyle, GUILayout.Height(46f))) return;

        _player.TakeDamage(0f, reactionType);
        _nextReactionTime = Time.unscaledTime + 1.5f;
        _status = $"{reactionType} playing";
    }

    private void CreateEnvironment()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        _cameraTransform = cameraObject.transform;

        new GameObject("TestCameraManager").AddComponent<CameraManager>();

        CreateCube("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(18f, 1f, 18f));
        CreateCube("BackWall", new Vector3(0f, 2f, 5f), new Vector3(18f, 5f, 1f));
        CreateCube("LeftWall", new Vector3(-8.5f, 2f, 0f), new Vector3(1f, 5f, 10f));
        CreateCube("RightWall", new Vector3(8.5f, 2f, 0f), new Vector3(1f, 5f, 10f));

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateCube(string name, Vector3 position, Vector3 scale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetPositionAndRotation(position, Quaternion.identity);
        cube.transform.localScale = scale;
    }

    private void EnsureStyles()
    {
        if (_panelStyle != null) return;

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(16, 16, 12, 12),
            fontSize = 18,
            alignment = TextAnchor.UpperCenter
        };
        _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
    }
}
