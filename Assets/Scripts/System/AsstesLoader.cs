using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


namespace Infrastructure
{
    /// <summary>
    /// Assetをロードするためのクラス
    /// </summary>
    public class AssetsLoader
    {
        /// <summary> Addressableを使ったAssetのLoadを行うメソッド </summary>
        /// <typeparam name="T"> 取得したいClass </typeparam>
        /// <param name="address"> 保存先のAddress </param>
        /// <returns> 取得結果 </returns>
        public static async UniTask<T> LoadAssetAsync<T>(string address) where T : class
        {
            if (!_handles.TryGetValue(address, out var handle))
            {
                var typedHandle = Addressables.LoadAssetAsync<T>(address);
                handle = typedHandle;
                _handles.Add(address, handle);
            }
            else
            {
                // 2回目以降の取得で即座に値が返り順序が変わるのを防ぐ
                await UniTask.Yield();
            }

            return await handle.Convert<T>().ToUniTask();
        }

        /// <summary>
        ///　リソースを開放する
        /// </summary>      
        /// <param name="address"></param>
        public static void Release(string address)
        {
            // addressの登録が見当たらなければ何もせずreturn
            if (!_handles.TryGetValue(address, out var handle)) return;

            Addressables.Release(_handles[address]);
            _handles.Remove(address);
        }

        private static readonly Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();
    }
}


