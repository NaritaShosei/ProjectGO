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

    public event Action OnFinishAllPhase;

    public BossEnemyPhaseChanger(BossEnemyDataHolder bossEnemyDataHolder)
    {
        _bossEnemyData = bossEnemyDataHolder;
    }

    public BossEnemyData CurrentPhaseBossData => _currentPhaseBossData;
    public int CurrentPhase => _currentPhase;

    /// <summary> 初期化 </summary>
    public void Init()
    {
        if (_bossEnemyData == null)
            Debug.LogError("BossのデータがNullです");

        _currentPhase = 0;

        ChangeNextPhase();
    }

    /// <summary> BossのPhaseをつぎのPhaseに移行する処理 </summary>
    public void ChangeNextPhase()
    {
        if (_bossEnemyData.BossEnemyDatas.Length <= _currentPhase)
        {   
            OnFinishAllPhase.Invoke();
            return;
        }

        _disposables.Clear();
        _currentPhaseBossData = _bossEnemyData.GetData(_currentPhase);

        if (_currentPhaseBossData == null) Debug.LogError("nullだよ");

        OnPhaseChange.Invoke(_currentPhaseBossData);

        _currentPhaseBossData.CurrentHP.Subscribe(hp =>
        {
            if(hp == 0) ChangeNextPhase();
        }).AddTo(_disposables);

        _currentPhase++;
    }

    private BossEnemyDataHolder _bossEnemyData = null;

    private BossEnemyData _currentPhaseBossData = null;

    private CompositeDisposable _disposables = new CompositeDisposable();

    private int _currentPhase = 0;
}
