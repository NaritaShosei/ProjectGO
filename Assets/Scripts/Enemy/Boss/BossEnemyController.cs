using BossEnemy.Data;
using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;
using static BossEnemyView;

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
        if(_bossEnemyView == null || _bossEnemyUIView == null)
        {
            Debug.LogError("Viewが設定されていません");
            return;
        }

        _deadEventDisposables = new();
        _phaseChangeEventDisposables = new();
        _phaseChanger = new(_bossEnemyMasterData);

        // イベントを登録
        _phaseChanger.OnPhaseChange += HandlePhaseChange;

        _phaseChanger.IsAllPhaseFinish.Subscribe(isFinish =>
        {
            if (isFinish)
            {
                HandleDead(_bossEnemyView);
            }
        }).AddTo(_deadEventDisposables);

        // PhaseChangeシステムを初期化
        _phaseChanger.Init();
    }

    /// <summary> 毎フレーム行われる処理を行うメソッド </summary>
    public void OnUpdate()
    {


    }

    /// <summary> ダメージを受けた際に呼ばれるメソッド </summary>
    public void HandleDamaged(DamageContext damageContext, BossEnemyPartsView hitParts)
    {
        int defense = 0;
        int damage = 0;

        if (_currentPhaseBossEnemyData == null) Debug.LogError("BossEnemyDataが設定されていません");

        if (hitParts.Armor == null || hitParts.Armor.IsBreak)
        {
            // 受けて個所によって防御力(肉質)を取得
            defense = DamageSystem.GetHitPartsDefense(hitParts.BossEnemyPartsType, _currentPhaseBossEnemyData);

            // Damageを計算
            if (hitParts.BossEnemyPartsType == BossEnemyPartsType.VitalPoint 
                || hitParts.BossEnemyPartsType == BossEnemyPartsType.WeekPoint)
            {
                // 弱点への攻撃ならPlayerもModeによるダメージの減増を行う
                damage = DamageSystem.CalculateDamage(defense, damageContext, true, EnemyDefenceType.Flesh);
            }
            else damage = DamageSystem.CalculateDamage(defense, damageContext);

            _currentPhaseBossEnemyData.TakeDamage(damage);
        }
        else
        {
            defense = DamageSystem.GetHitPartsArmorDefense(hitParts.Armor.AttachmentPoints, _currentPhaseBossEnemyData);

            damage = DamageSystem.CalculateDamage(defense, damageContext, true, EnemyDefenceType.Armor);

            switch (hitParts.Armor.AttachmentPoints)
            {
                case ArmorAttachmentPoint.LeftArm:
                    _currentPhaseBossEnemyData.LeftArmArmer.Damage(damage);
                    break;
                case ArmorAttachmentPoint.RightArm:
                    _currentPhaseBossEnemyData.RightArmArmer.Damage(damage);
                    break;
                case ArmorAttachmentPoint.LeftLeg:
                    _currentPhaseBossEnemyData.LeftLegArmer.Damage(damage);
                    break;
                case ArmorAttachmentPoint.RightLeg:
                    _currentPhaseBossEnemyData.RightLegArmer.Damage(damage);
                    break;
            }
        }
    }

    /// <summary> Viewを設定する </summary>
    public void SetView(BossEnemyView bossEnemyView, BossEnemyUIView bossEnemyUIView)
    {
        _bossEnemyView = bossEnemyView;
        _bossEnemyUIView = bossEnemyUIView;
    }

    [Header("BossEnemy全体のMasterData")]
    [SerializeField, Tooltip("BossEnemy全体のMasterData")]
    private TextAsset _csvBossEnemyMasterData = null;

    [Header("PhaseごとのEnemyのMasterData")]
    [SerializeField, Tooltip("PhaseごとのEnemyのMasterData")]
    private BossEnemyMasterData _bossEnemyMasterData = null;

    [Header("BossEnemyのAIBehaviorTree")]
    [SerializeField, Tooltip("BossEnemyのAI")]
    private BossEnemyBehaviorTree _bossEnemyBehaviorTree = null;

    // BossEnemyの各種ViewClass
    private BossEnemyView _bossEnemyView = null;
    private BossEnemyUIView _bossEnemyUIView = null;

    // BossEnemyの内部System
    private BossEnemyPhaseChanger _phaseChanger = null;
    private AttackDataRepository _attackDataRepositry = null;

    // BossEnemyのBehaviorTree
    private BossEnemyBehaviorTree _behaviorTree = null;

    // 現在のPhaseのBossEnemyData
    private BossEnemyData _currentPhaseBossEnemyData = null;

    private CompositeDisposable _deadEventDisposables = new CompositeDisposable();
    private CompositeDisposable _phaseChangeEventDisposables = new CompositeDisposable();

    /// <summary> Bossが死んだ際に呼ばれるメソッド </summary>
    private void HandleDead(IEnemy enemy)
    {
        _bossEnemyView.Dead(); 

        // event購読を終了
        _phaseChanger.OnPhaseChange -= HandlePhaseChange;
        _deadEventDisposables.Dispose();
        _phaseChangeEventDisposables.Dispose();
    }

    /// <summary> BossのPhaseが切り替わった際に呼ばれるイベント </summary>
    private void HandlePhaseChange(BossEnemyData bossEnemyData)
    {
        _phaseChangeEventDisposables.Clear();

        Debug.Log("PhaseChange");
        _currentPhaseBossEnemyData = bossEnemyData;
        _currentPhaseBossEnemyData.Init(_bossEnemyView.Self);

        // BehaviorTreeの初期化
        _bossEnemyBehaviorTree.Init(_currentPhaseBossEnemyData, _phaseChanger);

        // BossのTransformData変動時のイベント登録
        HandleBossTransform();

        // 各所鎧のHP変動時のイベント登録
        HandleArmorHPFluctuation();

        _bossEnemyView.ArmorInit();

        _bossEnemyUIView.PhaseChange(bossEnemyData, _phaseChanger.CurrentPhase);

        _currentPhaseBossEnemyData.CurrentHP.Subscribe(async hp =>
        {
            await _bossEnemyUIView.CurrentBar.TakeDamage(hp);
        }).AddTo(_phaseChangeEventDisposables);
    }

    /// <summary> 各所鎧のHP変動時のイベント </summary>
    private void HandleArmorHPFluctuation()
    {
        // 左腕の鎧のHP変動時
        _currentPhaseBossEnemyData.LeftArmArmer.CurrentHP.Subscribe(async hp =>
        {
            Debug.Log($"残りの左腕の鎧のHP： {hp}");
            ArmorHPFluctuationEventAction(hp, _currentPhaseBossEnemyData.LeftArmArmer, ArmorAttachmentPoint.LeftArm);
        }).AddTo(_phaseChangeEventDisposables);

        // 右腕の鎧のHP変動時
        _currentPhaseBossEnemyData.RightArmArmer.CurrentHP.Subscribe(async hp =>
        {
            Debug.Log($"残りの右腕の鎧のHP： {hp}");
            ArmorHPFluctuationEventAction(hp, _currentPhaseBossEnemyData.RightArmArmer, ArmorAttachmentPoint.RightArm);
        }).AddTo(_phaseChangeEventDisposables);

        // 左足の鎧のHP変動時
        _currentPhaseBossEnemyData.LeftLegArmer.CurrentHP.Subscribe(async hp =>
        {
            Debug.Log($"残りの左足の鎧のHP： {hp}");
            ArmorHPFluctuationEventAction(hp, _currentPhaseBossEnemyData.LeftLegArmer, ArmorAttachmentPoint.LeftLeg);
        }).AddTo(_phaseChangeEventDisposables);

        // 右足の鎧のHP変動時
        _currentPhaseBossEnemyData.RightLegArmer.CurrentHP.Subscribe(async hp =>
        {
            Debug.Log($"残りの右足の鎧のHP： {hp}");
            ArmorHPFluctuationEventAction(hp, _currentPhaseBossEnemyData.RightLegArmer, ArmorAttachmentPoint.RightLeg);
        }).AddTo(_phaseChangeEventDisposables);
    }

    private void HandleBossTransform()
    {
        // Data内のBossEnemyの座標が変わるたびにView側に変更を反映する
        _currentPhaseBossEnemyData.Position.Subscribe(position =>
        {
            _bossEnemyView.SetPosition(position);
        }).AddTo(_phaseChangeEventDisposables);

        // Data内のBossEnemyのQuaternionが変わるたびにView側に変更を反映する
        _currentPhaseBossEnemyData.Rotation.Subscribe(rotation =>
        {
            _bossEnemyView.SetRotation(rotation);
        }).AddTo(_phaseChangeEventDisposables);
    }


    private void ArmorHPFluctuationEventAction(int currentHP, BossArmorData armorData, ArmorAttachmentPoint attachmentPointsType)
    {
        if (currentHP == 0)
        {
            _bossEnemyView.ArmorBreak(attachmentPointsType);
            _bossEnemyBehaviorTree.HandleBossArmorBreak();
        }
        else if (armorData.IsArmorBreak)
        {
            _bossEnemyView.ArmorRepair(attachmentPointsType);
        }
    }
}
