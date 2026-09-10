using System.Collections.Generic;
using CriWare;
using UnityEngine;

/// <summary>
/// 指定した火事エフェクトの位置で環境音をループ再生する。
/// シーン進行による開始・終了は PlayFire / StopFire から制御できる。
/// </summary>
public class FireSoundPlayer : MonoBehaviour
{
    private const string CueSheetName = "Environment_SE";

    public void PlayFire()
    {
        StopFire();
        _isRequested = true;

        // 同じエフェクトを複数登録しても音量が重ならないようにする。
        var targets = new HashSet<Transform>();
        if (_effects == null) return;
        foreach (Transform effect in _effects)
        {
            if (effect != null && targets.Add(effect))
                _emitters.Add(new FireEmitter(effect));
        }
    }

    public void StopFire()
    {
        _isRequested = false;
        foreach (FireEmitter emitter in _emitters)
        {
            if (emitter.Source == null) continue;
            emitter.Source.Stop();
            Destroy(emitter.Source.gameObject);
        }
        _emitters.Clear();
    }

    [SerializeField, Tooltip("炎エフェクトのTransform。再生中に配列を変更した場合はPlayFireを再実行する。")]
    private Transform[] _effects;
    [SerializeField] private bool _playOnEnable = true;
    [SerializeField, Range(0f, 1f)] private float _volume = 0.25f;
    [SerializeField, Min(0f)] private float _minDistance = 2f;
    [SerializeField, Min(0.01f)] private float _maxDistance = 25f;

    private readonly List<FireEmitter> _emitters = new();
    private bool _isRequested;

    private void OnEnable()
    {
        if (_playOnEnable) PlayFire();
    }

    private void OnDisable()
    {
        StopFire();
    }

    private void Update()
    {
        if (!_isRequested) return;

        // システムシーンやキューシートの読み込みより先に有効化されても待機する。
        if (!ServiceLocator.TryGet(out SoundManager _)) return;
        var acb = CriAtom.GetAcb(CueSheetName);
        if (acb == null) return;
        if (!acb.GetCueInfo(SoundCueNames.Environment.VillageFire, out _))
        {
            Debug.LogWarning("Environment_SEにVillageFireがありません。", this);
            StopFire();
            return;
        }

        foreach (FireEmitter emitter in _emitters)
        {
            bool isVisible = emitter.Target != null && emitter.Target.gameObject.activeInHierarchy;
            if (!isVisible)
            {
                if (emitter.Source != null && emitter.IsPlaying) emitter.Source.Stop();
                emitter.IsPlaying = false;
                continue;
            }

            if (emitter.Source == null) emitter.Source = CreateSource(emitter.Target.position);
            emitter.Source.transform.position = emitter.Target.position;
            if (emitter.IsPlaying) continue;

            emitter.Source.Play();
            // Preparing中にPlayを繰り返すと再生開始できないため、要求済みとして管理する。
            emitter.IsPlaying = true;
        }
    }

    private CriAtomSource CreateSource(Vector3 position)
    {
        // エフェクトの既存音源には触れず、このコンポーネントが専用音源を所有する。
        var sourceObject = new GameObject("VillageFire Audio");
        sourceObject.transform.SetParent(transform, false);
        sourceObject.transform.position = position;
        var source = sourceObject.AddComponent<CriAtomSource>();
        source.playOnStart = false;
        source.cueSheet = CueSheetName;
        source.cueName = SoundCueNames.Environment.VillageFire;
        source.loop = true;
        source.volume = _volume;
        source.use3dPositioning = true;
        // SoundManagerと同様にリスナーを固定せず、CRIのアクティブリスナー自動選択を使う。
        // カメラ切替などによる既存リスナーの有効・無効にも追従する。
        source.player.SetPanType(CriAtomEx.PanType.Pos3d);
        source.attenuationDistanceSetting = true;
        source.source.SetMinMaxDistance(_minDistance, Mathf.Max(_minDistance + 0.01f, _maxDistance));
        source.source.Update();
        return source;
    }

    private sealed class FireEmitter
    {
        public readonly Transform Target;
        public CriAtomSource Source;
        public bool IsPlaying;

        public FireEmitter(Transform target)
        {
            Target = target;
        }
    }
}
