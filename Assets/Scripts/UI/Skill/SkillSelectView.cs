using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

/// <summary>
/// スキル選択UI全体を管理するView。
/// 各ボタンからの「選択された」「クリックされた」という通知を受け取り、
/// どのボタンの演出を再生し、どのボタンの演出を止めるかを一元管理する。
/// </summary>
public class SkillSelectView : MonoBehaviour, ISkillSelectView
{
    /// <summary>
    /// スキル選択が確定したときに通知する。
    /// Presenterはこの通知を受けて、実際のスキル獲得処理を行う。
    /// </summary>
    public event Action<int> OnSkillSelected;

    /// <summary>
    /// ハイライト中のスキルが切り替わったときに通知する。
    /// Presenterは自動選択時にこのIDを優先して選ぶ。
    /// </summary>
    public event Action<int> OnSkillHighlighted;

    /// <summary>
    /// スキル選択UIを表示し、候補スキルを各ボタンへ反映する。
    /// 前回表示時の購読や演出が残らないよう、最初に必ず全ボタンを初期化する。
    /// </summary>
    public void Show(List<SkillViewData> skills)
    {
        ResolveReferences();

        if (_buttons == null || _buttons.Length == 0)
        {
            Debug.LogError($"{nameof(SkillSelectView)}のボタン参照が設定されていません", this);
            return;
        }

        StopAllButtons();
        UnsubscribeButtons();
        _buttonMap.Clear();

        // ボタン数より候補スキル数が少ない場合、余ったボタンは非表示にする。
        // 逆に候補スキル数が多い場合は、用意されているボタン数まで表示する。
        for (int i = 0; i < _buttons.Length; i++)
        {
            var button = _buttons[i];
            if (button == null) continue;

            if (i < skills.Count)
            {
                button.gameObject.SetActive(true);
                button.Setup(skills[i]);
                button.OnHighlighted += OnButtonHighlighted;
                button.OnClicked += OnButtonClicked;
                _buttonMap[skills[i].Id] = button;
            }
            else
            {
                button.ResetState();
                button.gameObject.SetActive(false);
            }
        }

        _panel.SetActive(true);

        // 表示直後は先頭候補を選択状態にする。
        // これにより、時間切れの自動選択でも「現在選択中のスキル」を自然に選べる。
        if (skills.Count > 0 && _buttonMap.TryGetValue(skills[0].Id, out var firstButton))
        {
            SelectHighlightedButton(firstButton, skills[0].Id);
            EventSystem.current?.SetSelectedGameObject(firstButton.gameObject);
        }
    }

    /// <summary>
    /// スキル選択UIを閉じる。
    /// 非表示にするだけでなく、演出停止とイベント購読解除もここでまとめて行う。
    /// </summary>
    public void Hide()
    {
        ResolveReferences();

        _panel.SetActive(false);
        StopAllButtons();
        UnsubscribeButtons();
        _buttonMap.Clear();
    }

    [SerializeField] private GameObject _panel;
    [FormerlySerializedAs("_buttonArray")]
    [SerializeField] private SkillSelectButton[] _buttons;

    /// <summary>
    /// スキルIDから対応するボタンを引くための辞書。
    /// クリック演出や自動選択など、IDからボタンへ戻したい場面で使う。
    /// </summary>
    private readonly Dictionary<int, SkillSelectButton> _buttonMap = new();

    /// <summary>
    /// Inspector参照が未設定でも動けるよう、起動時に参照解決を試みる。
    /// </summary>
    private void Awake()
    {
        ResolveReferences();
    }

    /// <summary>
    /// ボタンから選択通知を受け取ったときの入口。
    /// 実際の演出切り替えは <see cref="SelectHighlightedButton"/> に集約する。
    /// </summary>
    private void OnButtonHighlighted(SkillSelectButton button, int skillId)
    {
        SelectHighlightedButton(button, skillId);
    }

    /// <summary>
    /// ボタンからクリック通知を受け取ったときの入口。
    /// クリックされたボタンを選択状態にしたうえで、クリック演出完了後に選択確定を通知する。
    /// </summary>
    private void OnButtonClicked(SkillSelectButton button, int skillId)
    {
        SelectHighlightedButton(button, skillId);
        button.PlayClick(() => OnSkillSelected?.Invoke(skillId));
    }

    /// <summary>
    /// 指定したボタンだけを選択状態にし、それ以外のボタンの選択演出を止める。
    /// 「前のボタンの演出が残り続ける」問題を避けるため、演出制御はこのメソッドに集約する。
    /// </summary>
    private void SelectHighlightedButton(SkillSelectButton targetButton, int skillId)
    {
        if (targetButton == null) return;
        if (_buttons == null) return;

        foreach (var button in _buttons)
        {
            if (button == null) continue;

            if (button == targetButton)
            {
                button.PlayHighlight();
            }
            else
            {
                button.ForceStopHighlight();
            }
        }

        OnSkillHighlighted?.Invoke(skillId);
    }

    /// <summary>
    /// 登録されている全ボタンの演出と見た目を初期状態に戻す。
    /// UIを閉じるとき、再表示前、未使用ボタンを隠す前に呼ぶ。
    /// </summary>
    private void StopAllButtons()
    {
        if (_buttons == null) return;

        foreach (var button in _buttons)
        {
            if (button == null) continue;

            button.ResetState();
        }
    }

    /// <summary>
    /// ボタンイベントの購読を解除する。
    /// Showのたびに購読し直すため、解除しないと同じイベントが多重に呼ばれてしまう。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (_buttons == null) return;

        foreach (var button in _buttons)
        {
            if (button == null) continue;

            button.OnHighlighted -= OnButtonHighlighted;
            button.OnClicked -= OnButtonClicked;
        }
    }

    /// <summary>
    /// Inspector参照を補完する。
    /// Prefabやシーン側の参照が空でも、子にあるSkillSelectButtonを自動収集して動作できるようにする。
    /// </summary>
    private void ResolveReferences()
    {
        if (_panel == null)
        {
            _panel = gameObject;
        }

        if (_buttons == null || _buttons.Length == 0)
        {
            _buttons = GetComponentsInChildren<SkillSelectButton>(true);
        }
    }
}
