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
        var temporaryPath = path + ".tmp";
        var json = JsonUtility.ToJson(data, true);

        try
        {
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        Debug.Log($"[SaveLoadService] 保存完了: {path}");
    }

    public static T Load<T>() where T : new()
    {
        var path = GetPath<T>();

        if (!File.Exists(path))
        {
            // 初回起動時は型の初期値をそのままゲーム設定として使用する。
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
            // 破損データで起動不能にならないよう、読込失敗時も初期値へフォールバックする。
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
        // 型ごとにファイルを分けるため、型名をそのままファイル名として使用する。
        return Path.Combine(Application.persistentDataPath, typeof(T).Name + Extension);
    }
}
