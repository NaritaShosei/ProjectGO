using UnityEngine;

/// <summary> Sound関連の初期化クラス </summary>
public class CRIAudioInitializer : MonoBehaviour
{
    [SerializeField, Header("BGMを鳴らすGameObject")] 
    private GameObject _bgmPlayer;

    [SerializeField, Header("デフォルトのBGMシート名")] 
    private string _deaultBGMCueSheet = "BGM";

    private SoundManager _soundManager;

    private void Awake()
    {
        // SoundManagerをServiceLocatorに登録
        _soundManager = new SoundManager(_bgmPlayer, _deaultBGMCueSheet);
        ServiceLocator.Register(_soundManager);
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
