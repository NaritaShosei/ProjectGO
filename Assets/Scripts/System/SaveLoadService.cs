using System;
using System.IO;
using UnityEngine;

/// <summary>
/// persistentDataPath配下のJSONファイルへデータを保存・読込する汎用サービス。
/// </summary>
public static class SaveLoadService
{
    public static void Save<T>(T data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        var path = GetPath<T>();
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        Debug.Log($"[SaveLoadService] 保存完了: {path}");
    }

    public static T Load<T>() where T : new()
    {
        var path = GetPath<T>();

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveLoadService] セーブデータが見つかりません: {path} → 新規生成");
            return new T();
        }

        try
        {
            var json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<T>(json);
            return data == null ? new T() : data;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SaveLoadService] 読込に失敗しました: {path} → 新規生成\n{exception.Message}");
            return new T();
        }
    }

    public static T Reset<T>() where T : new()
    {
        var data = new T();
        Save(data);
        Debug.Log($"[SaveLoadService] リセット完了: {GetPath<T>()}");
        return data;
    }

    private const string Extension = ".json";

    private static string GetPath<T>()
    {
        return Path.Combine(Application.persistentDataPath, typeof(T).Name + Extension);
    }
}
