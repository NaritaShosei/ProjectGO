using UnityEngine;

/// <summary> Sound関連の初期化クラス </summary>
public class CRIAudioInitializer : MonoBehaviour
{
    [SerializeField, Header("BGMを鳴らすGameObject")] 
    private GameObject _bgmPlayer;

    [SerializeField, Header("デフォルトのBGMシート名")] 
    private string _deaultBGMCueSheet = "BGM";

    private void Awake()
    {
        // SoundManagerをServiceLocatorに登録
        ServiceLocator.Register(new SoundManager(_bgmPlayer, _deaultBGMCueSheet));
    }

    private void OnDestroy()
    {
        // SoundManagerをServiceLocatorから削除
        ServiceLocator.Unregister<SoundManager>();
    }
}
