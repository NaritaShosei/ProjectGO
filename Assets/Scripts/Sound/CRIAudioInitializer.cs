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

    private SoundManager _soundManager;

    private void Awake()
    {
        // SoundManagerをServiceLocatorに登録
        _soundManager = new SoundManager(_bgmPlayer, _deaultBGMCueSheet);
        _soundManager.SetBGMFadeDurations(_bgmFadeInSeconds, _bgmFadeOutSeconds);
        ServiceLocator.Register(_soundManager);
    }

    private void OnValidate()
    {
        _bgmFadeInSeconds = Mathf.Clamp(_bgmFadeInSeconds, 0f, 3600f);
        _bgmFadeOutSeconds = Mathf.Clamp(_bgmFadeOutSeconds, 0f, 3600f);
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
