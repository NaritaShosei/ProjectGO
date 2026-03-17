using System;
using UnityEngine;

/// <summary> ゲーム内で使用する音をInspectorで設定するためのクラス </summary>
public class AudioHolder : MonoBehaviour
{
    #region Audio Controllers
    [Serializable]
    public class BGMController
    {
        [SerializeField]
        private BGMType _bgmType = default;
        [SerializeField]
        private AudioClip _clip = default;

        public BGMType BGMType => _bgmType;
        public AudioClip Clip => _clip;
    }

    [Serializable]
    public class SEController
    {
        [SerializeField]
        private SEType _seType = default;
        [SerializeField]
        private AudioClip _clip = default;

        public SEType SEType => _seType;
        public AudioClip Clip => _clip;
    }
    #endregion

    [SerializeField]
    private BGMController[] _bgmClips = default;
    [SerializeField]
    private SEController[] _seClips = default;

    public BGMController[] BGMClips => _bgmClips;
    public SEController[] SEClips => _seClips;
}

public enum BGMType
{
    None,
    Title,
    InGame,
}

public enum SEType
{
    None,
    TestSE,
}
