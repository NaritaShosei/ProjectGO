using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// AddressableGroupを自動的に割り当て、グループ名を定義してあるクラスを生成する
/// </summary>
public class AddressableGroupDetectorWindow : EditorWindow
{
    private AddressableAssetGroup _targetGroup;
    private const string _saveFolder = "Assets/Scripts/AddressableAssetPath";

    [MenuItem("Tools/AddressableGroupDetectorWindow")]
    private static void Init()
    {
        CreateWindow<AddressableGroupDetectorWindow>().Show();
    }

    private void OnGUI()
    {
        _targetGroup = (AddressableAssetGroup)EditorGUILayout.ObjectField(_targetGroup, typeof(AddressableAssetGroup), false);

        if (_targetGroup == null) return;
        var path = _saveFolder + $"/AAG{_targetGroup.name.Replace(" ", "")}.cs";

        if (GUILayout.Button("Generate"))
        {
            if (!File.Exists(path))
            {
                File.Create(path);
            }

            List<string> content = new List<string>();
            content.Add("// 自動生成のソースコードです\n");
            content.Add($"public class AAG{_targetGroup.name.Replace(" ", "")}" + "\n{\n");
            foreach (var obj in _targetGroup.entries)
            {
                var line =
                    $"    public const string k{obj.AssetPath.Split('.')[0].Replace("/", "_")} = \"{obj.AssetPath}\";\n\n";
                content.Add(line);
            }

            content.Add("}\n");
            WriteCode(path, content);
        }
    }

    private void WriteCode(string path, List<string> content)
    {
        try
        {
            // StreamWriterを使用して、ファイルを確実に閉じる
            using (StreamWriter writer = new StreamWriter(path, false, System.Text.Encoding.UTF8))
            {
                foreach (var line in content)
                {
                    writer.Write(line);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"Successfully generated: {path}");
        }
        catch (IOException ex)
        {
            Debug.LogError($"Failed to write {path}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error writing {path}: {ex.Message}");
        }
    }
}
