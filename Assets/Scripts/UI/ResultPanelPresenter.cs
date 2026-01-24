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

        _resultPanelView.SetClearWaveCount(resultData.ClearWaveCountText);
        // 戦績
        _resultPanelView.SetKillCount(resultData.KillCountText);
        _resultPanelView.SetComboCount(resultData.ComboCountText);
        _resultPanelView.SetDamageCount(resultData.DamageCountText);
        _resultPanelView.SetTakeDamageCount(resultData.TakeDamageCountText);
        _resultPanelView.SetHealingCount(resultData.HealingCountText);
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
