using Interface;
/// <summary>
/// プレイヤーモードの状態をUIへ反映する
/// </summary>
public class PlayerModePresenter
{
    public PlayerModePresenter(IModeController modeController,
        IPlayerModeView view)
    {
        _modeController = modeController;
        _view = view;
    }
    public void Initialize()
    {
        // 初期表示
        _view.SetMode(_modeController.CurrentMode);

        // モード変更通知を購読
        _modeController.OnModeChanged += OnModeChanged;
    }
    public void Dispose()
    {
        _modeController.OnModeChanged -= OnModeChanged;
    }


    private readonly IModeController _modeController;
    private readonly IPlayerModeView _view;


    private void OnModeChanged(PlayerMode mode)
    {
        _view.SetMode(mode);
    }
}
