using System;

/// <summary>
/// ThunderGauge の変化を GaugeView へ反映する Presenter。
/// InGameUIInitializer でインスタンス化し、OnDestroy で Dispose する。
/// </summary>
public class ThunderGaugePresenter : IDisposable
{
    public ThunderGaugePresenter(ThunderGauge model, GaugeView view)
    {
        _model = model;
        _view = view;

        // 起動時に現在値を即反映
        _view.UpdateGauge(_model.Current, _model.Max, _model.Max);

        _model.OnChanged += HandleChanged;
    }

    public void Dispose()
    {
        _model.OnChanged -= HandleChanged;
    }

    private readonly ThunderGauge _model;
    private readonly GaugeView _view;

    private void HandleChanged(float current, float max)
    {
        // 雷ゲージは最大値が変化しない仕様のため initialMax = max で固定
        _view.UpdateGauge(current, max, max);
    }
}
