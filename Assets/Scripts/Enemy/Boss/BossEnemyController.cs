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
    public void Init(EnemyServices enemyServices)
    {
        _phaseChanger = new(_bossEnemyData);

        // イベントを登録
        _bossEnemyView.OnDead += HandleDead;
        _phaseChanger.OnPhaseChange += HandlePhaseChange;
        _phaseChanger.OnFinishAllPhase += _bossEnemyView.Dead;

        // 初期化
        _phaseChanger.Init();
    }

    /// <summary> 毎フレーム行われる処理を行うメソッド </summary>
    public void OnUpdate()
    {
        if(_currentPhaseBehaviorTree != null) 
            _currentPhaseBehaviorTree.OnUpdate();

    }

    /// <summary> ダメージを受けた際に行うメソッド </summary>
    public void HandleDamaged(DamageContext damageContext, BossEnemyPartsType hitParts)
    {
        // 受けて個所によって防御力(肉質)を取得
        int defense = DamageSystem.GetHitPartsDefense(hitParts, _currentPhaseBossEnemyData);

        // 受けたダメージの合計
        int damage = 0;

        // Damageを計算
        if (hitParts == BossEnemyPartsType.VitalPoint || hitParts == BossEnemyPartsType.WeekPoint)
        {
            damage = DamageSystem.CalculateDamage(defense, damageContext, true, EnemyDefenceType.Flesh);
        }
        else
        {
            damage = DamageSystem.CalculateDamage(defense, damageContext);
        }

        _currentPhaseBossEnemyData.TakeDamage(damage);
    }

    /// <summary> Viewを設定する </summary>
    public void SetView(BossEnemyView bossEnemyView, BossEnemyUIView bossEnemyUIView)
    {
        _bossEnemyView = bossEnemyView;
        _bossEnemyUIView = bossEnemyUIView;
    }

    [Header("PhaseごとのEnemyのMasterData")]
    [SerializeField, Tooltip("PhaseごとのEnemyのMasterData")]
    private BossEnemyDataHolder _bossEnemyData = null;

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
        _phaseChanger.OnFinishAllPhase -= _bossEnemyView.Dead;
    }

    private void HandlePhaseChange(BossEnemyData bossEnemyData)
    {
        Debug.Log("PhaseChange");
        _currentPhaseBossEnemyData = bossEnemyData;
        _currentPhaseBossEnemyData.Init(_bossEnemyView.Self);

        // Data内のBossEnemyの座標が変わるたびにView側に変更を反映する
        _currentPhaseBossEnemyData.Position.Subscribe(position =>
        {
            _bossEnemyView.SetPosition(position);
        }).AddTo(_disposables);

        _bossEnemyUIView.PhaseChange(bossEnemyData, _phaseChanger.CurrentPhase);

        _currentPhaseBossEnemyData.CurrentHP.Subscribe(async hp =>
        {
            _bossEnemyUIView.CurrentBar.TakeDamage(hp).Forget();
        }).AddTo(_disposables);
    }
}
