using BossEnemy.BehaviorTree;
using BossEnemy.Data;
using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class BossEnemyController
{
    /// <summary> 現在のPhaseのBossEnemyData </summary>
    public BossEnemyData CurrentBossData => _currentPhaseBossEnemyData;

    /// <summary> 初期化 </summary>
    /// <param name="bossEnemyView"> BossのViewClass </param>
    /// <param name="enemyServices"> Enemy共通のServicesClass </param>
    public void Init(BossEnemyView bossEnemyView, BossEnemyUIView bossEnemyUIView, EnemyServices enemyServices)
    {
        _bossEnemyView = bossEnemyView;
        _bossEnemyUIView = bossEnemyUIView;

        _phaseChanger = new(_bossEnemyDatas);
        _phaseChanger.Init();

        // イベントを登録
        _phaseChanger.OnPhaseChange += HandlePhaseChange;
    }

    /// <summary> 毎フレーム行われる処理を行うメソッド </summary>
    public void OnUpdate()
    {
        if(_currentPhaseBehaviorTree != null) 
            _currentPhaseBehaviorTree.OnUpdate();


    }

    public void HandleDamaged(DamageContext damageContext)
    {
        
    }

    [Header("PhaseごとのEnemyのMasterData")]
    [SerializeField, Tooltip("PhaseごとのEnemyのMasterData")]
    private BossEnemyData[] _bossEnemyDatas = null;

    // BossEnemyのViewClass
    private BossEnemyView _bossEnemyView = null;
    private BossEnemyUIView _bossEnemyUIView = null;

    // BossのPhase変更機構
    private BossEnemyPhaseChanger _phaseChanger = null;

    // BossEnemyのBehaviorTree
    private BossEnemyBehaviorTree _currentPhaseBehaviorTree = null;

    // 現在のPhaseのBossEnemyData
    private BossEnemyData _currentPhaseBossEnemyData = null;

    private CompositeDisposable _disposables = new CompositeDisposable();

    private void HandleDead(IEnemy enemy)
    {
        // event購読を終了
        _bossEnemyView.OnDead -= HandleDead;
        _phaseChanger.OnPhaseChange -= HandlePhaseChange;
    }

    private void HandlePhaseChange(BossEnemyData bossEnemyData)
    {
        bossEnemyData.Init(_bossEnemyView.GetTargetCenter());

        _currentPhaseBossEnemyData.Position.Subscribe(position =>
        {
            _bossEnemyView.SetPosition(position);
        }).AddTo(_disposables);
    }
}
