using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// コントローラー振動を確認するための専用テストUI。
/// </summary>
public sealed class ControllerVibrationTestUI : MonoBehaviour
{
    private float _lowFrequency = 0.25f;
    private float _highFrequency = 0.75f;
    private float _duration = 1f;
    private Coroutine _vibrationCoroutine;

    private void OnGUI()
    {
        var width = Mathf.Min(620f, Screen.width - 40f);
        var height = 460f;
        var area = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

        GUI.Box(area, GUIContent.none);
        GUILayout.BeginArea(new Rect(area.x + 30f, area.y + 24f, area.width - 60f, area.height - 48f));

        GUILayout.Label("Controller Vibration Test", TitleStyle);
        GUILayout.Space(16f);

        var gamepad = Gamepad.current;
        GUILayout.Label(gamepad == null
            ? "Gamepad: Not Connected"
            : $"Gamepad: {gamepad.displayName}", StatusStyle(gamepad != null));

        GUILayout.Space(22f);
        _lowFrequency = DrawSlider("Low Frequency", _lowFrequency, "重さ・揺れを感じる低周波モーター");
        _highFrequency = DrawSlider("High Frequency", _highFrequency, "細かい振動を感じる高周波モーター");
        _duration = DrawSlider("Duration", _duration, "振動時間（秒）", 0.1f, 5f, "F1");

        GUILayout.Space(24f);
        GUI.enabled = gamepad != null;
        if (GUILayout.Button("Play Vibration", ButtonStyle, GUILayout.Height(52f)))
        {
            PlayVibration();
        }

        GUI.enabled = true;
        if (GUILayout.Button("Stop", ButtonStyle, GUILayout.Height(42f)))
        {
            StopVibration();
        }

        GUILayout.EndArea();
    }

    private float DrawSlider(
        string label,
        float value,
        string description,
        float min = 0f,
        float max = 1f,
        string format = "F2")
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, LabelStyle, GUILayout.Width(190f));
        value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Height(24f));
        GUILayout.Label(value.ToString(format), LabelStyle, GUILayout.Width(52f));
        GUILayout.EndHorizontal();
        GUILayout.Label(description, DescriptionStyle);
        GUILayout.Space(12f);
        return value;
    }

    private void PlayVibration()
    {
        StopVibration();
        _vibrationCoroutine = StartCoroutine(VibrateForDuration());
    }

    private IEnumerator VibrateForDuration()
    {
        Gamepad.current?.SetMotorSpeeds(_lowFrequency, _highFrequency);
        yield return new WaitForSecondsRealtime(_duration);
        StopVibration();
    }

    private void StopVibration()
    {
        if (_vibrationCoroutine != null)
        {
            StopCoroutine(_vibrationCoroutine);
            _vibrationCoroutine = null;
        }

        Gamepad.current?.ResetHaptics();
    }

    private void OnDisable()
    {
        StopVibration();
    }

    private static GUIStyle TitleStyle => new(GUI.skin.label)
    {
        alignment = TextAnchor.MiddleCenter,
        fontSize = 28,
        fontStyle = FontStyle.Bold
    };

    private static GUIStyle LabelStyle => new(GUI.skin.label)
    {
        fontSize = 17
    };

    private static GUIStyle DescriptionStyle => new(GUI.skin.label)
    {
        fontSize = 13,
        normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
    };

    private static GUIStyle ButtonStyle => new(GUI.skin.button)
    {
        fontSize = 18,
        fontStyle = FontStyle.Bold
    };

    private static GUIStyle StatusStyle(bool connected)
    {
        return new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            normal = { textColor = connected ? Color.green : new Color(1f, 0.45f, 0.35f) }
        };
    }
}
