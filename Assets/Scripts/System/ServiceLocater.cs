using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シンプルなサービスロケーターパターンの実装
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();
    private static readonly object _lock = new();

    /// <summary>
    /// サービスを登録します
    /// </summary>
    /// <typeparam name="T">サービスの型</typeparam>
    /// <param name="service">登録するサービスインスタンス</param>
    /// <param name="allowOverwrite">既存のサービスの上書きを許可するか</param>
    /// <exception cref="ArgumentNullException">serviceがnullの場合</exception>
    /// <exception cref="InvalidOperationException">既に登録されており、上書きが許可されていない場合</exception>
    public static void Register<T>(T service, bool allowOverwrite = false) where T : class
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        var type = typeof(T);

        // 競合状態が起きないように複数スレッドからの同時アクセスを防ぐ
        lock (_lock)
        {
            if (_services.ContainsKey(type))
            {
                if (!allowOverwrite)
                {
                    throw new InvalidOperationException($"{type.Name}は既に登録されています");
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[ServiceLocator] {type.Name}を上書きしました");
#endif
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else
            {
                Debug.Log($"[ServiceLocator] {type.Name}を登録しました");
            }
#endif

            _services[type] = service;
        }
    }

    /// <summary>
    /// サービスを取得します
    /// </summary>
    /// <typeparam name="T">サービスの型</typeparam>
    /// <returns>登録されているサービス</returns>
    /// <exception cref="InvalidOperationException">サービスが登録されていない場合</exception>
    public static T Get<T>() where T : class
    {
        var type = typeof(T);

        lock (_lock)
        {
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
        }

        throw new InvalidOperationException($"{type.Name}は登録されていません");
    }

    /// <summary>
    /// サービスの取得を試みます
    /// </summary>
    /// <typeparam name="T">サービスの型</typeparam>
    /// <param name="service">取得したサービス</param>
    /// <returns>サービスが登録されている場合はtrue</returns>
    public static bool TryGet<T>(out T service) where T : class
    {
        var type = typeof(T);

        lock (_lock)
        {
            if (_services.TryGetValue(type, out var obj))
            {
                service = (T)obj;
                return true;
            }
        }

        service = null;
        return false;
    }

    /// <summary>
    /// サービスが登録されているか確認します
    /// </summary>
    /// <typeparam name="T">サービスの型</typeparam>
    /// <returns>登録されている場合はtrue</returns>
    public static bool IsRegistered<T>() where T : class
    {
        lock (_lock)
        {
            return _services.ContainsKey(typeof(T));
        }
    }

    /// <summary>
    /// サービスを登録解除します
    /// </summary>
    /// <typeparam name="T">サービスの型</typeparam>
    /// <returns>登録解除に成功した場合はtrue</returns>
    public static bool Unregister<T>() where T : class
    {
        var type = typeof(T);

        lock (_lock)
        {
            if (_services.Remove(type))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ServiceLocator] {type.Name}を削除しました");
#endif
                return true;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"[ServiceLocator] {type.Name}は登録されていません");
#endif
        return false;
    }
}