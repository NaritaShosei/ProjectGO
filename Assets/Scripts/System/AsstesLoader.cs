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
            AsyncOperationHandle<T> addressableHandle = default;
            if (!_handles.ContainsKey(address))
            {
                addressableHandle = Addressables.LoadAssetAsync<T>(address);
                _handles.Add(address, addressableHandle);
            }
            else
            {
                // 2回目以降の取得で即座に値が返り順序が変わるのを防ぐ
                await UniTask.Yield();
                return (T)_handles[address].Result;
            }
            await addressableHandle.Task;
            return addressableHandle.Result;
        }

        /// <summary> Labelを使ったアセットのロードを行うメソッド </summary>
        /// <typeparam name="T"> 取得したいクラス </typeparam>
        /// <param name="labelReference"> Labelの情報 </param>
        /// <returns> 取得したObject </returns>
        private static async UniTask<List<T>> LoadAssetAsyncWithLabel<T>(AssetLabelReference labelReference) where T : class
        {
            List<T> result = new List<T>();

            var handle = Addressables.LoadAssetsAsync<Object>(labelReference, null);
            await handle.ToUniTask();

            foreach (var item in handle.Result)
            {
                T castItem = item as T;

                if(castItem != null) result.Add(castItem);
            }

            return result;
        }

        /// <summary>
        ///　リソースを開放する
        /// </summary>      
        /// <param name="address"></param>
        public static void Release(string address)
        {
            Addressables.Release(_handles[address]);
            _handles.Remove(address);
        }

        private static readonly Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();
    }
}


