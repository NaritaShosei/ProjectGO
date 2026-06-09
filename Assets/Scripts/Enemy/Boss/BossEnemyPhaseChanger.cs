using BossEnemy.Data;
using System;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

[Serializable]
/// <summary> BossEnemyのPhase変更クラス </summary>
public class BossEnemyPhaseChanger
{
    public event Action<BossEnemyData> OnPhaseChange;

    public BossEnemyPhaseChanger(BossEnemyData[] bossEnemyDatas)
    {
        _bossEnemyDatas = bossEnemyDatas;
    }

    public BossEnemyData CurrentPhaseBossData => _currentPhaseBossData;
    public int CurrentPhase => _currentPhase;

    /// <summary> 初期化 </summary>
    public void Init()
    {
        if (_bossEnemyDatas == null)
            Debug.LogError("BossのデータがNullです");

        _currentPhase = 0;
    }

    /// <summary> BossのPhaseをつぎのPhaseに移行する処理 </summary>
    public void ChangeNextPhase()
    {
        if (_bossEnemyDatas.Length >= _currentPhase) return;

        _disposables.Clear();
        _currentPhaseBossData = _bossEnemyDatas[_currentPhase];

        _currentPhaseBossData.CurrentHP.Subscribe(hp =>
        {
            if(hp == 0) ChangeNextPhase();
            OnPhaseChange.Invoke(_currentPhaseBossData);
        }).AddTo(_disposables);

        _currentPhase++;
    }

    private BossEnemyData[] _bossEnemyDatas = null;

    private BossEnemyData _currentPhaseBossData = null;

    private CompositeDisposable _disposables = new CompositeDisposable();

    private int _currentPhase = 0;
}
