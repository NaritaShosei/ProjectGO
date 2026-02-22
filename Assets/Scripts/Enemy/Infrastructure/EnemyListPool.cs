using System.Collections.Generic;

/// <summary>
/// 本来はいろんなところで使ってもらえるが、一応Enemyのために用意したのでEnemyListPool
/// GCの削減のために準備した
/// </summary>
/// <typeparam name="T"></typeparam>
public static class EnemyListPool<T>
{
    // 32個分先に予約している。最大制限はまだ設定していない。
    private static readonly Stack<List<T>> _pool = new Stack<List<T>>(32);

    /// <summary>
    /// List を取得（なければ新規生成）
    /// </summary>
    public static List<T> Get()
    {
        if (_pool.Count > 0)
        {
            return _pool.Pop();
        }
        return new List<T>();
    }

    /// <summary>
    /// List をプールへ返却
    /// </summary>
    public static void Release(List<T> list)
    {
        if (list == null) return;

        list.Clear();
        _pool.Push(list);
    }
}
