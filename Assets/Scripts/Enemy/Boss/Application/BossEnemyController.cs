using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;

# region BossEnemy関連のusing
using BossEnemy.BehaviorTree;
using BossEnemy.Data;
using BossEnemy.Enum;
using BossEnemy.Data.Repositry;
using BossEnemy.Model.CoreLogic;
using BossEnemy.Data.Repository;
# endregion


[Serializable]
public class BossEnemyController
{
    public BossEnemyData CurrentBossData => _currentPhaseBossEnemyData.Value;

    /// <summary> 初期化 </summary>
    /// <param name="bossEnemyView"> BossのViewClass </param>
    /// <param name="enemyServices"> Enemy共通のServicesClass </param>
    public void Init(EnemyServices enemyServices, IAnimationEventReceiver bossEnemyAnimationEventReceiver)
    {
        if(_bossEnemyView == null || _bossEnemyUIView == null)
        {
            Debug.LogError("Viewが設定されていません");
            return;
        }

        // Animationによるイベントの通知クラスを取得
        _enemyAnimationEventReceiver = bossEnemyAnimationEventReceiver;

        // BossEnemyのマスターデータを取得
        _bossEnemyMasterDataRepository = new();
        _bossEnemyMasterDataRepository.Init(_csvBossEnemyMasterData.text);
        _bossEnemyMasterData = _bossEnemyMasterDataRepository.GetData(_id);

        // 各種ロジックを初期化
        _takeDamage = new();
        _bossMove = new();
        _bossAttack = new(enemyServices.PlayerInformationService, _attackCoolTimer);
        _bossDown = new();

        // BossMoveにTimeScaleを反映
        _bossEnemyView.TimeScaleProperty.Subscribe(timeScale =>
        { 
            _bossMove.SetTimeScale(timeScale); 
        }).AddTo(_deadEventDisposables);

        // BehaviorTreeの初期化
        _bossEnemyBehaviorTree.Init(_bossAttack, _bossMove, _bossDown, _attackCoolTimer);

        // Enemyがうけられるサービス一覧
        _enemyServices = enemyServices;

        // Phase切り替えシステムを初期化
        _phaseChange = new(_bossEnemyMasterData);

        // 全てのPhaseが終了した際のイベントを登録
        _phaseChange.OnFinishAllPhase += _deadEventDisposables.Dispose;
        _phaseChange.OnFinishAllPhase += () => HandleDead(_bossEnemyView);
        _phaseChange.OnFinishAllPhase += _bossEnemyBehaviorTree.HandleDead;

        // 攻撃データのリポジトリの初期化
        _attackDataRepositry = new(_csvBossEnemyMasterData);

        RegisterPhaseChangeEventAction();
        RegisterBossDataChangeEventAction();
        RegisterBossAttackEventAction();
        RegisterTakeDamageEventAction();
        RegisterBossDownEventAction();

        // 最初のPhaseを開始
        _phaseChange.StartFirstPhase();
    }

    /// <summary> Viewを設定する </summary>
    public void SetView(BossEnemyView bossEnemyView, BossEnemyUIView bossEnemyUIView)
    {
        _bossEnemyView = bossEnemyView;
        _bossEnemyUIView = bossEnemyUIView;
    }

    /// <summary> 毎フレーム行われる処理を行うメソッド </summary>
    public void OnUpdate()
    {
        _bossEnemyBehaviorTree.OnUpdate();
    }

    [Header("BossEnemy全体のMasterData")]
    [SerializeField, Tooltip("BossEnemy全体のMasterData")]
    private TextAsset _csvBossEnemyMasterData = null;

    [Header("BossEnemyのAIBehaviorTree")]
    [SerializeField, Tooltip("BossEnemyのAI")]
    private BossEnemyBehaviorTree _bossEnemyBehaviorTree = null;

    [Header("生成するBossのID")]
    [SerializeField, Tooltip("生成するBossのID")]
    private int _id = 1;

    private BossEnemyMasterData _bossEnemyMasterData = null;

    // BossEnemyの各種View
    private BossEnemyView _bossEnemyView = null;
    private BossEnemyUIView _bossEnemyUIView = null;

    // BossEnemyの内部ロジック
    private BossAttack _bossAttack = null;
    private BossMove _bossMove = null;
    private Damage _takeDamage = null;
    private PhaseChange _phaseChange = null;
    private BossDown _bossDown = null;

    // 各種リポジトリ
    private BossEnemyAttackDataRepositry _attackDataRepositry = null;
    private BossEnemyMasterDataRepository _bossEnemyMasterDataRepository = null;

    // 現在のPhaseのBossEnemyData
    private ReactiveProperty<BossEnemyData> _currentPhaseBossEnemyData = new();

    // BossEnemyの攻撃のクールタイム
    private AttackCoolTimer _attackCoolTimer = new();

    // BossEnemyの受けられるサービス
    private EnemyServices _enemyServices;

    // Animationによるイベントの通知クラス
    private IAnimationEventReceiver _enemyAnimationEventReceiver = null;

    // 複数の非同期処理やイベントイベントの購読管理・解除Class
    private CompositeDisposable _deadEventDisposables = new CompositeDisposable();
    private CompositeDisposable _phaseChangeEventDisposables = new CompositeDisposable();

    /// <summary> Bossが死んだ際に呼ばれるメソッド </summary>
    private void HandleDead(IEnemy enemy)
    {
        _deadEventDisposables.Dispose();
        _phaseChangeEventDisposables.Dispose();

        _bossEnemyView.Dead();

        // event購読を終了
        _enemyAnimationEventReceiver.OnAttackHit -= _bossAttack.Hit;
        _bossEnemyView.OnTakeDamage -= _takeDamage.TakeDamage;
        _enemyAnimationEventReceiver.OnAttackEnd -= _bossEnemyView.AttackEnd;
        _enemyAnimationEventReceiver.OnAttackEnd -= _bossAttack.Finish;
        _phaseChange.OnPhaseChanged -= _bossEnemyView.ArmorInit;
        _phaseChange.OnPhaseChanged -= _bossEnemyView.PhaseChange;
        _phaseChange.OnPhaseChanged -= HandlePhaseChange;
        _bossDown.OnDown -= _bossEnemyView.Down;
        _bossDown.OnRiseUp -= _bossEnemyView.RiseUp;
        _bossAttack.OnAttackStart -= _bossEnemyView.Attack;
        _enemyAnimationEventReceiver.OnColliderIsTriggerIsEnabled -= _bossMove.ColliderIsTrigger;
        _enemyAnimationEventReceiver.OnMove -= _bossMove.MoveTargetPositionRightOnTime;
        _enemyAnimationEventReceiver.OnAttackHit -= _bossAttack.Hit;
    }

    private void RegisterBossDownEventAction()
    {
        _bossDown.OnDown += _bossEnemyView.Down;
        _bossDown.OnRiseUp += _bossEnemyView.RiseUp;
    }

    /// <summary> BossのPhaseが切り替わった際に呼ばれるイベント </summary>
    private void RegisterPhaseChangeEventAction()
    {
        _phaseChange.OnPhaseChanged += HandlePhaseChange;
        _phaseChange.OnPhaseChanged += _bossEnemyView.ArmorInit;
        _phaseChange.OnPhaseChanged += _bossEnemyView.PhaseChange;
    }

    private void HandlePhaseChange()
    {
        Debug.Log("Phaseが変わりました");

        _phaseChangeEventDisposables.Dispose();
        _phaseChangeEventDisposables = new();

        _phaseChange.CurrentPhaseBossData.Init(_bossEnemyView.Self);
        _currentPhaseBossEnemyData.Value = _phaseChange.CurrentPhaseBossData;
    }

    private void RegisterBossDataChangeEventAction()
    {
        _currentPhaseBossEnemyData.Subscribe(data =>
        {
            if (data == null) return;

            // BossUIとの連動
            _bossEnemyUIView.PhaseChange(data, _phaseChange.CurrentPhase);

            // 移動処理とのDataの連動
            _bossMove.SetBossEnemy(data);

            // BossDataとViewのTransformを連動
            RegisterBossMoveEventAction();

            // ダメージロジックの初期化
            _takeDamage.Init(data);

            // 鎧破壊の初期化
            _bossDown.Init(data);

            // 各所鎧のHP変動時のイベント登録
            RegisterArmorDamageEventAction();

            // BehaviorTreeのPhase切り替え処理
            _bossEnemyBehaviorTree.HandlePhaseChanged(data, _attackDataRepositry, _enemyServices.PlayerInformationService);


            // HPが0になったときフェーズを切り替える処理
            data.CurrentHP.Subscribe(hp =>{ if (hp == 0)_phaseChange.ChangeNextPhase();  }).AddTo(_phaseChangeEventDisposables);

            // Bossの衝突判定のオンオフ
            data.IsTigger.Subscribe(isTrigger => { _bossEnemyView.SetIsTrigger(isTrigger); }).AddTo(_phaseChangeEventDisposables);

        }).AddTo(_deadEventDisposables);
    }

    private void RegisterBossMoveEventAction()
    {
        // Data内のBossEnemyの座標が変わるたびにView側に変更を反映する
        _currentPhaseBossEnemyData.Value.Position.Subscribe(position => 
        { _bossEnemyView.SetPosition(position); }).AddTo(_phaseChangeEventDisposables);

        // Data内のBossEnemyのQuaternionが変わるたびにView側に変更を反映する
        _currentPhaseBossEnemyData.Value.Rotation.Subscribe(rotation =>
        { _bossEnemyView.SetRotation(rotation); }).AddTo(_phaseChangeEventDisposables);

        // Data内のBossEnemyのVelocityが変わるたびにView側に変更を反映する
        _currentPhaseBossEnemyData.Value.Velocity.Subscribe(velocity =>
        { _bossEnemyView.SetVelocity(velocity); }).AddTo(_phaseChangeEventDisposables);
    }

    public void RegisterBossAttackEventAction()
    {
        // 攻撃開始時
        _bossAttack.OnAttackStart += _bossEnemyView.Attack;

        // 攻撃中ColliderIsTrigger時
        _enemyAnimationEventReceiver.OnColliderIsTriggerIsEnabled += _bossMove.ColliderIsTrigger;

        // 攻撃中移動時
        _enemyAnimationEventReceiver.OnMove += _bossMove.MoveTargetPositionRightOnTime;

        // 攻撃ヒット時
        _enemyAnimationEventReceiver.OnAttackHit += _bossAttack.Hit;

        // 攻撃終了時
        _enemyAnimationEventReceiver.OnAttackEnd += _bossEnemyView.AttackEnd;
        _enemyAnimationEventReceiver.OnAttackEnd += _bossAttack.Finish;
    }

    public void RegisterTakeDamageEventAction()
    {
        _bossEnemyView.OnTakeDamage += _takeDamage.TakeDamage;
    }

    /// <summary> 各所鎧のHP変動時のイベント </summary>
    private void RegisterArmorDamageEventAction()
    {
        // 左腕の鎧のHP変動時
        _currentPhaseBossEnemyData.Value.LeftArmArmer.CurrentHP.Subscribe(hp =>
        {
            ArmorHPFluctuationEventAction(hp, _currentPhaseBossEnemyData.Value.LeftArmArmer, ArmorAttachmentPoint.LeftArm);
        }).AddTo(_phaseChangeEventDisposables);

        // 右腕の鎧のHP変動時
        _currentPhaseBossEnemyData.Value.RightArmArmer.CurrentHP.Subscribe(hp =>
        {
            ArmorHPFluctuationEventAction(hp, _currentPhaseBossEnemyData.Value.RightArmArmer, ArmorAttachmentPoint.RightArm);
        }).AddTo(_phaseChangeEventDisposables);

        // 左足の鎧のHP変動時
        _currentPhaseBossEnemyData.Value.LeftLegArmer.CurrentHP.Subscribe(hp =>
        {
            ArmorHPFluctuationEventAction(hp, _currentPhaseBossEnemyData.Value.LeftLegArmer, ArmorAttachmentPoint.LeftLeg);
        }).AddTo(_phaseChangeEventDisposables);

        // 右足の鎧のHP変動時
        _currentPhaseBossEnemyData.Value.RightLegArmer.CurrentHP.Subscribe(hp =>
        {
            ArmorHPFluctuationEventAction(hp, _currentPhaseBossEnemyData.Value.RightLegArmer, ArmorAttachmentPoint.RightLeg);
        }).AddTo(_phaseChangeEventDisposables);
    }

    private void ArmorHPFluctuationEventAction(int currentHP, BossArmorData armorData, ArmorAttachmentPoint attachmentPointsType)
    {
        if (currentHP == 0)
        {
            _bossEnemyView.ArmorBreak(attachmentPointsType);
            _bossEnemyBehaviorTree.HandleBossArmorBreak(attachmentPointsType);
        }
        else if (armorData.IsArmorBreak)
        {
            _bossEnemyView.ArmorRepair(attachmentPointsType);
        }
    }
}
