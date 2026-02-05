using Interface;
/// <summary>
/// プレイヤーモードの状態をUIへ反映するPresenter
/// </summary>
public class PlayerModePresenter
{
    /// <summary>
    /// Presenter の生成時に「モード管理」と「UI表示」を受け取る
    /// </summary>
    public PlayerModePresenter(IModeController modeController,
        IPlayerModeView view)
    {
        _modeController = modeController;
        _view = view;
    }

    /// <summary>
    /// 初期化処理
    /// </summary>
    public void Initialize()
    {
        // 起動時のUI初期表示
        _view.SetMode(_modeController.CurrentMode);

        // モード変更通知
        _modeController.OnModeChanged += OnModeChanged;
    }

    public void Dispose()
    {
        _modeController.OnModeChanged -= OnModeChanged;
    }

    private readonly IModeController _modeController;
    private readonly IPlayerModeView _view;

    /// <summary>
    /// モードが変更された時に呼ばれる
    /// </summary>
    private void OnModeChanged(PlayerMode mode)
    {
        //モードをUIに反映
        _view.SetMode(mode);
    }
}
