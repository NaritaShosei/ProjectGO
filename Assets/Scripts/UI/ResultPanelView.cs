using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class ResultPanelView : MonoBehaviour
{
    public event Action OnShowOverview;
    public event Action OnShowRecord;
    public event Action OnShowBuild;
    public event Action OnTransitionToTitle;

    [Header("パネル設定")]
    [SerializeField] private GameObject _overviewPanel;
    [SerializeField] private GameObject _recordPanel;
    [SerializeField] private GameObject _buildPanel;

    [Header("概要")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _clearWaveCount;
    [SerializeField] private Button _showRecordButton;

    [Header("戦績")]
    [SerializeField] private TextMeshProUGUI _killCount;
    [SerializeField] private TextMeshProUGUI _comboCount;
    [SerializeField] private TextMeshProUGUI _damageCount;
    [SerializeField] private TextMeshProUGUI _takeDamageCount;
    [SerializeField] private TextMeshProUGUI _healingCount;
    [SerializeField] private Button _showBuildButton;
    [SerializeField] private Button _backOverviewButton;

    [Header("ビルド構成")]
    [SerializeField] private TextMeshProUGUI _buildBalance;
    [SerializeField] private TextMeshProUGUI _skillList;
    [SerializeField] private TextMeshProUGUI _finalStatas;
    [SerializeField] private Button _transitionToTitleButton;
    [SerializeField] private Button _backRecordButton;

    // Presenter向けの設定適用メソッド
    public void SetTitleText(string text) => _titleText.text = text;
    public void SetClearWaveCount(string text) => _clearWaveCount.text = text;
    public void SetKillCount(string text) => _killCount.text = text;
    public void SetComboCount(string text) => _comboCount.text = text;
    public void SetDamageCount(string text) => _damageCount.text = text;
    public void SetTakeDamageCount(string text) => _takeDamageCount.text = text;
    public void SetHealingCount(string text) => _healingCount.text = text;
    public void SetBuildBalance(string text) => _buildBalance.text = text;
    public void SetSkillList(string text) => _skillList.text = text;
    public void SetFinalStats(string text) => _finalStatas.text = text;

    // パネル表示メソッド(選択も自動で行う)
    public void ShowOverviewPanel()
    {
        EventSystem.current.SetSelectedGameObject(_showRecordButton.gameObject);
    }

    public void ShowRecordPanel()
    {
        EventSystem.current.SetSelectedGameObject(_showBuildButton.gameObject);
    }

    public void ShowBuildPanel()
    {
        EventSystem.current.SetSelectedGameObject(_transitionToTitleButton.gameObject);
    }

    private void Start()
    {
        _showRecordButton.onClick.AddListener(() => OnShowRecord?.Invoke());
        _showBuildButton.onClick.AddListener(() => OnShowBuild?.Invoke());
        _backOverviewButton.onClick.AddListener(() => OnShowOverview?.Invoke());
        _backRecordButton.onClick.AddListener(() => OnShowRecord?.Invoke());
        _transitionToTitleButton.onClick.AddListener(() => OnTransitionToTitle?.Invoke());
    }

    private void OnDestroy()
    {
        _showRecordButton.onClick.RemoveAllListeners();
        _showBuildButton.onClick.RemoveAllListeners();
        _backOverviewButton.onClick.RemoveAllListeners();
        _backRecordButton.onClick.RemoveAllListeners();
        _transitionToTitleButton.onClick.RemoveAllListeners();
    }
}