using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ResultPanelView : MonoBehaviour
{
    public event Action OnShowOverview;
    public event Action OnShowRecord;
    public event Action OnShowBuild;
    public event Action OnTransitionToTitle;

    // Presenter向けの設定適用メソッド
    // 概要
    public void SetTitleText(string text) => _titleText.text = text;
    public void SetClearWaveCount(string text) => _clearWaveCount.text = text;

    // 戦績
    public void SetKillCount(string text) => _killCount.text = text;
    public void SetComboCount(string text) => _comboCount.text = text;
    public void SetDamageCount(string text) => _damageCount.text = text;
    public void SetTakeDamageCount(string text) => _takeDamageCount.text = text;
    public void SetHealingCount(string text) => _healingCounr.text = text;

    // ビルド構成
    public void SetBuildBalance(string text) => _buildBalance.text = text;
    public void SetSkillList(string text) => _skillList.text = text;
    public void SetFinalStats(string text) => _FinalStatass.text = text;

    [Header("概要")]
    [SerializeField]
    private TextMeshProUGUI _titleText;
    [SerializeField]
    private TextMeshProUGUI _clearWaveCount;
    [SerializeField]
    private Button _showRecordButton;

    [Header("戦績")]
    [SerializeField]
    private TextMeshProUGUI _killCount;
    [SerializeField]
    private TextMeshProUGUI _comboCount;
    [SerializeField]
    private TextMeshProUGUI _damageCount;
    [SerializeField]
    private TextMeshProUGUI _takeDamageCount;
    [SerializeField]
    private TextMeshProUGUI _healingCounr;
    [SerializeField]
    private Button _showBuildButton;
    [SerializeField]
    private Button _buckOverviewButton;

    [Header("ビルド構成")]
    [SerializeField]
    private TextMeshProUGUI _buildBalance;
    [SerializeField]
    private TextMeshProUGUI _skillList;
    [SerializeField]
    private TextMeshProUGUI _FinalStatass;
    [SerializeField]
    private Button _transitionToTitleButton;
    [SerializeField]
    private Button _buckRecordButton;

    private void Start()
    {
        _showRecordButton.onClick.AddListener (() => OnShowRecord?.Invoke());
        _showBuildButton.onClick.AddListener (() => OnShowBuild?.Invoke());
        _buckOverviewButton.onClick.AddListener (() => OnShowOverview?.Invoke());
        _buckRecordButton.onClick.AddListener (() => OnShowRecord?.Invoke());
        _transitionToTitleButton.onClick.AddListener (() => OnTransitionToTitle?.Invoke());
    }

    private void OnDestroy()
    {
        _showRecordButton.onClick.RemoveAllListeners();
        _showBuildButton.onClick.RemoveAllListeners();
        _buckOverviewButton.onClick.RemoveAllListeners();
        _buckRecordButton.onClick.RemoveAllListeners();
        _transitionToTitleButton.onClick.RemoveAllListeners();
    }
}
