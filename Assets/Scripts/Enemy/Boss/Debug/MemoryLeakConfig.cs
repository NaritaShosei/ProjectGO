using UnityEngine;

public class MemoryLeakConfig : MonoBehaviour
{
    void Awake()
    {
        // Unity 6でネイティブコンテナ（NativeArray等）のリーク検出モードを最大にする
        Unity.Collections.NativeLeakDetection.Mode = Unity.Collections.NativeLeakDetectionMode.EnabledWithStackTrace;

        Debug.Log("Leak Detection Mode has been set to EnabledWithStackTrace.");
    }
}
