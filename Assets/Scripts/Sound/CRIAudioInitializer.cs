using UnityEngine;

/// <summary> Sound関連の初期化クラス </summary>
public class CRIAudioInitializer : MonoBehaviour
{
    [SerializeField, Header("BGMを鳴らすGameObject")] 
    private GameObject _bgmPlayer;

    [SerializeField, Header("デフォルトのBGMシート名")] 
    private string _deaultBGMCueSheet = "InGame_BGM";

    private void Awake()
    {
        // SoundManagerをServiceLocatorに登録
        ServiceLocator.Register(new SoundManager(_bgmPlayer, _deaultBGMCueSheet));
    }
}
