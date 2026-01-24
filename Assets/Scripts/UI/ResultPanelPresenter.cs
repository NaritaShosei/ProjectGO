using System;

public class ResultPanelPresenter
{
    public event Action OnShowOverview;
    public event Action OnShowRecord;
    public event Action OnShowBuild;
    public event Action OnTransitionToTitle;

    public ResultPanelPresenter(ResultPanelView view, ResultPanelModel model)
    {
        this._resultPanelView = view;
        this._resultPanelModel = model;
        // イベント登録
        view.OnShowOverview += HandleShowOverview;
        view.OnShowRecord += HandleShowRecord;
        view.OnShowBuild += HandleShowBuild;
        view.OnTransitionToTitle += HandleTransitionToTitle;
        InitializeResultPanel();
    }

    private ResultPanelView _resultPanelView;
    private ResultPanelModel _resultPanelModel;
    private void InitializeResultPanel()
    {
        // Modelから結果データを取得してUIに反映
        var resultData = _resultPanelModel.GetResultData();

        // 概要
        if (resultData.IsCleared)
        {
            _resultPanelView.SetTitleText("Cleared");
        }
        else
        {
            _resultPanelView.SetTitleText("GameOver");
        }

        _resultPanelView.SetClearWaveCount($"突破ウェーブ数: {resultData.ClearWaveCount.ToString()}");
        // 戦績
        _resultPanelView.SetKillCount($"撃破数: {resultData.KillCount.ToString()}");
        _resultPanelView.SetComboCount($"最大コンボ数: {resultData.ComboCount.ToString()}");
        _resultPanelView.SetDamageCount($"累計与ダメージ: {resultData.DamageCount.ToString()}");
        _resultPanelView.SetTakeDamageCount($"累計被ダメージ: {resultData.TakeDamageCount.ToString()}");
        _resultPanelView.SetHealingCount($"累計回復量: {resultData.HealingCount.ToString()}");
        // ビルド構成
        _resultPanelView.SetBuildBalance(resultData.BuildBalanceText);
        _resultPanelView.SetSkillList(resultData.SkillListText);
        _resultPanelView.SetFinalStats(resultData.FinalStatsText);
    }

    private void HandleShowOverview()
    {
        OnShowOverview?.Invoke();
    }

    private void HandleShowRecord()
    {
        OnShowRecord?.Invoke();
    }

    private void HandleShowBuild()
    {
        OnShowBuild?.Invoke();
    }

    private void HandleTransitionToTitle()
    {
        OnTransitionToTitle?.Invoke();
    }
}
