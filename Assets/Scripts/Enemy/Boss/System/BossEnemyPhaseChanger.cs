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

    public BossEnemyPhaseChanger(BossEnemyMasterData bossEnemyDataHolder)
    {
        _bossEnemyMasterData = bossEnemyDataHolder;
    }

    public IReadOnlyReactiveProperty<bool> IsAllPhaseFinish => _isAllPhaseFinish;
    public BossEnemyData CurrentPhaseBossData => _currentPhaseBossData;
    public int CurrentPhase => _currentPhase;

    /// <summary> 初期化 </summary>
    public void Init()
    {
        if (_bossEnemyMasterData == null)
            Debug.LogError("Bossのデータがnullです");

        ChangeNextPhase(true);
    }

    /// <summary> BossのPhaseを次のPhaseに移行する処理 </summary>
    public void ChangeNextPhase(bool isFirstPhase = false)
    {
        if (isFirstPhase) _currentPhase = 0;
        else _currentPhase++;

        Debug.Log(_currentPhase);

        if (_bossEnemyMasterData.BossEnemyDatas.Length <= _currentPhase)
        {
            _isAllPhaseFinish.Value = true;
            _disposables.Dispose();
            return;
        }

        _disposables.Clear();

        _currentPhaseBossData = _bossEnemyMasterData.GetData(_currentPhase);

        OnPhaseChange?.Invoke(_currentPhaseBossData);

        _currentPhaseBossData.CurrentHP.Subscribe(hp =>
        {
            if(hp == 0)
            {
                ChangeNextPhase();
            }
        }).AddTo(_disposables);
    }

    private ReactiveProperty<bool> _isAllPhaseFinish = new ReactiveProperty<bool>(false);

    private BossEnemyMasterData _bossEnemyMasterData = null;

    private BossEnemyData _currentPhaseBossData = null;

    private CompositeDisposable _disposables = new CompositeDisposable();

    private int _currentPhase = 0;
}
