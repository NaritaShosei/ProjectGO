using UnityEngine;

public sealed class SubtitleController : MonoBehaviour
{
    public void InitializeController(SubtitleSettings settings, Player player, SubtitleView view)
    {
        _settings = settings;
        // Player直下の原点に置いて移動に追従させる。専用の所有者で効果音と停止処理を分離する。
        _voiceOwner = new GameObject("SubtitleVoice");
        _voiceOwner.transform.SetParent(player != null ? player.transform : transform, false);
        if (_settings == null || view == null)
        {
            Debug.LogError("[SubtitleController] 字幕設定または事前配置したSubtitleViewが未設定です。", this);
            return;
        }
        if (!view.InitializeView()) return;
        _view = view;
        _soundHandler = player != null ? player.GetComponent<PlayerSoundHandler>() : null;
        if (_soundHandler != null)
            _soundHandler.OnReviveVoicePlayed += HandleReviveVoice;
    }

    public void SetSequence(SequenceStateType sequence)
    {
        // 待機中の字幕と音声も破棄し、ムービースキップやゲームオーバー後に遅れて出さない。
        HideSubtitle();
        _sequence = sequence;
    }

    public void PlayVoiceSubtitle(string cueName)
    {
        ShowSubtitle(cueName, _voiceOwner);
    }

    public void ShowSubtitle(string cueName, GameObject voiceOwner = null)
    {
        if (!isActiveAndEnabled) return;
        HideSubtitle();
        SubtitleLine line = _settings != null ? _settings.GetLine(cueName) : null;
        if (line == null)
        {
            // 字幕設定の欠落によって既存ボイスまで失われないようにする。
            if (voiceOwner != null) Sound.PlaySE(voiceOwner, cueName, CueSheetType.PlayerVoice);
            return;
        }
        _view?.SetText(line.Text);
        _timeline = new SubtitleTimeline(line.Delay, line.Duration, line.FadeIn, line.FadeOut);
        _pendingVoiceOwner = voiceOwner;
        _pendingCue = cueName;
        UpdateSubtitle(0f);
    }

    public void HideSubtitle()
    {
        if (_voiceOwner != null) Sound.StopSE(_voiceOwner);
        ClearSubtitle();
    }

    private SubtitleSettings _settings;
    private SubtitleView _view;
    private PlayerSoundHandler _soundHandler;
    private SubtitleTimeline _timeline;
    private SequenceStateType _sequence;
    private GameObject _voiceOwner;
    private GameObject _pendingVoiceOwner;
    private string _pendingCue;

    private void ClearSubtitle()
    {
        _timeline = null;
        _pendingVoiceOwner = null;
        _pendingCue = null;
        _view?.HideSubtitle();
    }

    private void Update()
    {
        UpdateSubtitle(Time.unscaledDeltaTime);
    }

    private void UpdateSubtitle(float deltaTime)
    {
        if (_timeline == null) return;
        _timeline.AdvanceTime(deltaTime);
        if (_timeline.HasStarted && _pendingVoiceOwner != null)
        {
            Sound.PlaySE(_pendingVoiceOwner, _pendingCue, CueSheetType.PlayerVoice);
            _pendingVoiceOwner = null;
        }
        if (_timeline.IsComplete)
        {
            // 表示時間は音声の長さとは独立。字幕終了だけではボイスを切らない。
            ClearSubtitle();
            return;
        }
        _view?.SetAlpha(_timeline.GetAlpha());
    }

    private void HandleReviveVoice(string cueName)
    {
        // 音声側で選んだキューをそのまま使い、ランダム結果と字幕の食い違いを防ぐ。
        if (_sequence == SequenceStateType.MobAndSkill) ShowSubtitle(cueName);
    }

    private void OnDisable()
    {
        HideSubtitle();
    }

    private void OnDestroy()
    {
        if (_soundHandler != null)
            _soundHandler.OnReviveVoicePlayed -= HandleReviveVoice;
        // Playerが残る場合も、コントローラーが作成した音源を残さない。
        if (_voiceOwner != null) Destroy(_voiceOwner);
    }
}
