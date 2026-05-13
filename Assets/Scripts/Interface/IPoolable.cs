/// <summary>
/// オブジェクトプールで管理されるコンポーネントに実装するインターフェース。
/// GenericObjectPool はこのインターフェースを実装した Component のみを対象とする。
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// プールから取り出された直後に呼ばれる。
    /// 状態のリセットや初期化処理を実装する。
    /// </summary>
    void OnGet();

    /// <summary>
    /// プールへ返却される直前に呼ばれる。
    /// 後始末（イベント解除・Tween停止など）を実装する。
    /// </summary>
    void OnRelease();
}
