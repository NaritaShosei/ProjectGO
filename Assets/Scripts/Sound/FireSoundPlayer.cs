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
        Sound.StopLoopSE(gameObject, SoundCueNames.Environment.VillageFire);
    }

    [SerializeField] private bool _playOnEnable = true;
    // TODO: Atom Craft側でVillageFireがSEカテゴリ未割り当てのため、暫定的にここで直接音量調整する。
    // カテゴリ割り当てが直ったらSEカテゴリの音量・ダッキングに委ねてこのフィールドは削除する。
    [SerializeField, Range(0f, 1f)] private float _volume = 0.25f;
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

        // SoundManager経由で再生し、SEカテゴリの音量・ダッキングを他のSEと同様に受けられるようにする。
        var source = Sound.PlayLoopSE(gameObject, SoundCueNames.Environment.VillageFire, CueSheetType.Environment, use3dPositioning: false);
        if (source != null) source.volume = _volume;
    }
}
