using System;
using System.Threading;
using CriWare;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>ステージ全体に火事の環境音を1つの2D音源で流す。</summary>
public class FireSoundPlayer : MonoBehaviour
{
    private const string CueSheetName = "Environment_SE";
    private const float LoadCheckInterval = 0.25f;

    public void PlayFire()
    {
        StopFire();
        _isRequested = true;
        if (!isActiveAndEnabled) return;
        _loadCancellation = new CancellationTokenSource();
        LoadSourceAsync(_loadCancellation.Token).Forget();
    }

    public void StopFire()
    {
        _isRequested = false;
        // 再開始・無効化時に、以前のロード待ちが音源を生成しないよう中断する。
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = null;
        if (_source != null) _source.Stop();
    }

    [SerializeField] private bool _playOnEnable = true;
    [SerializeField, Range(0f, 1f)] private float _volume = 0.25f;
    private CriAtomSource _source;
    private CancellationTokenSource _loadCancellation;
    private bool _isRequested;

    private void OnEnable()
    {
        if (_playOnEnable || _isRequested) PlayFire();
    }

    private void OnDisable()
    {
        StopFire();
    }

    private async UniTask LoadSourceAsync(CancellationToken cancellationToken)
    {
        CriAtomExAcb acb;
        // 起動順に依存しないよう、準備待ちの間だけ確認する。
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ServiceLocator.TryGet(out SoundManager _))
            {
                acb = CriAtom.GetAcb(CueSheetName);
                if (acb != null) break;
            }
            // Time.timeScaleが0でもシステムのロード完了を確認する。
            await UniTask.Delay(TimeSpan.FromSeconds(LoadCheckInterval),
                ignoreTimeScale: true, cancellationToken: cancellationToken);
        }

        if (!acb.GetCueInfo(SoundCueNames.Environment.VillageFire, out _))
        {
            Debug.LogWarning("Environment_SEにVillageFireがありません。", this);
            return;
        }

        if (_source == null)
        {
            // 他の音源を上書きせず、再開始時にもこの専用音源1つを再利用する。
            _source = gameObject.AddComponent<CriAtomSource>();
            _source.playOnStart = false;
            _source.cueSheet = CueSheetName;
            _source.cueName = SoundCueNames.Environment.VillageFire;
            _source.loop = true;
            _source.use3dPositioning = false;
            // キュー側が3D設定でも距離減衰しないよう、再生方式を明示する。
            _source.player.SetPanType(CriAtomEx.PanType.Pan3d);
        }
        _source.volume = _volume;
        _source.Play();
    }
}
