using UnityEngine;

/// <summary> Sound関連の初期化クラス </summary>
public class CRIAudioInitializer : MonoBehaviour
{
    [SerializeField, Header("BGMを鳴らすGameObject")] 
    private GameObject _bgmPlayer;

    [SerializeField, Header("デフォルトのBGMシート名")] 
    private string _deaultBGMCueSheet = "BGM";

    [SerializeField, Min(0f), Header("BGMフェード設定（秒・起動時に適用）"), Tooltip("0で即時再生")]
    private float _bgmFadeInSeconds = 1f;
    [SerializeField, Min(0f), Tooltip("0で即時停止")]
    private float _bgmFadeOutSeconds = 1f;

#if UNITY_EDITOR
    [SerializeField, Range(0f, 1f), Header("音量（Play中の動作確認用・セーブされません・エディタ専用）")]
    private float _bgmVolume = 0.5f;
    [SerializeField, Range(0f, 1f)]
    private float _seVolume = 0.5f;
    [SerializeField, Range(0f, 1f)]
    private float _voiceVolume = 0.5f;

    private float _appliedBgmVolume;
    private float _appliedSeVolume;
    private float _appliedVoiceVolume;
#endif

    private SoundManager _soundManager;

    private void Awake()
    {
        // SoundManagerをServiceLocatorに登録
        _soundManager = new SoundManager(_bgmPlayer, _deaultBGMCueSheet);
        _soundManager.SetBGMFadeDurations(_bgmFadeInSeconds, _bgmFadeOutSeconds);
        ServiceLocator.Register(_soundManager);

#if UNITY_EDITOR
        // OnValidateはPlay開始時に確実に呼ばれるとは限らないため、起動時の反映はここで行う。
        // 実機ビルドには含めず、GameSettingService経由の正規の音量だけが反映されるようにする。
        _soundManager.SetBGMVolume(_bgmVolume);
        _soundManager.SetSEVolume(_seVolume);
        _soundManager.SetVoiceVolume(_voiceVolume);

        // OnValidateでの差分検知用に初期値をキャッシュ
        _appliedBgmVolume = _bgmVolume;
        _appliedSeVolume = _seVolume;
        _appliedVoiceVolume = _voiceVolume;
#endif
    }

    private void OnValidate()
    {
        _bgmFadeInSeconds = Mathf.Clamp(_bgmFadeInSeconds, 0f, 3600f);
        _bgmFadeOutSeconds = Mathf.Clamp(_bgmFadeOutSeconds, 0f, 3600f);

#if UNITY_EDITOR
        if (!Application.isPlaying || _soundManager == null) return;

        // Play中にInspectorで動かした値をその場で反映する（設定画面の値とは別系統）。
        // 音量以外のフィールド変更で無関係な音量が再適用されないよう、変化した項目のみ反映する。
        if (!Mathf.Approximately(_bgmVolume, _appliedBgmVolume))
        {
            _soundManager.SetBGMVolume(_bgmVolume);
            _appliedBgmVolume = _bgmVolume;
        }

        if (!Mathf.Approximately(_seVolume, _appliedSeVolume))
        {
            _soundManager.SetSEVolume(_seVolume);
            _appliedSeVolume = _seVolume;
        }

        if (!Mathf.Approximately(_voiceVolume, _appliedVoiceVolume))
        {
            _soundManager.SetVoiceVolume(_voiceVolume);
            _appliedVoiceVolume = _voiceVolume;
        }
#endif
    }

    private void OnDestroy()
    {
        // SoundManagerをServiceLocatorから削除
        if (ServiceLocator.TryGet(out SoundManager current) &&
            ReferenceEquals(current, _soundManager))
        {
            ServiceLocator.Unregister<SoundManager>();
        }
    }
}
