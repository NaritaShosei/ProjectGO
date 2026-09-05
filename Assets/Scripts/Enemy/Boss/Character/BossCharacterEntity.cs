using BossEnemy.Armor;
using BossEnemy.Attack;
using BossEnemy.Enum;
using BossEnemy.Interface;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

// BossEnemyに関するData
namespace BossEnemy.Character
{
    # region CharacterInterface
    public interface IBossCharacterEntity : IMovement
    {
        public event Action OnArmorBreak;

        public event Action OnDead;

        /// <summary> ボスの名前 </summary>
        public string BossName { get; }

        /// <summary> 攻撃標的 </summary>
        public IPlayer AttackTarget { get; }

        public Attack.AttackData ExecutingAttackData { get; }

        public int CurrentAttackSelectPoolID { get; }

        /// <summary> 現在のHP </summary>
        public IReadOnlyReactiveProperty<int> CurrentHP { get; }

        /// <summary> 現在の姿勢 </summary>
        public IReadOnlyReactiveProperty<PostureType> CurrentCharacterPostureType { get; }

        /// <summary> Phase切り替え中フラグ </summary>
        public IReadOnlyReactiveProperty<bool> IsPhaseChaging { get; }

        /// <summary> 攻撃中フラグ </summary>
        public IReadOnlyReactiveProperty<bool> IsAttacking { get; }

        /// <summary> ボスのタイムスケール </summary>
        public float TimeScale { get; }

        /// <summary> キャラクターの現在のステータス </summary>
        public CharacterStatus CharacterCurrentStats { get; }

        /// <summary> 全Phaseのキャラクターステータス </summary>
        public CharacterStatus[] AllPhaseCharacterStats { get; }

        /// <summary> 各種鎧の現在のHP状況 </summary>
        public IReadOnlyDictionary<ArmorAttachmentType, int> ArmorCurrentHPDict { get; }

        /// <summary> 初期化 </summary>
        public void Init();

        /// <summary> 生成(スポーン)された際の処理 </summary>
        public void Spawn(IPlayer firstTarget, Transform characterTransform);

        /// <summary> 装備中の鎧のステータスを取得する </summary>
        /// <param name="armorAttachmentType"> 取得したい鎧の種類 </param>
        public ArmorStatus GetArmorStats(ArmorAttachmentType armorAttachmentType);

        /// <summary> 装備中の全ての鎧のステータスを取得する </summary>
        public IReadOnlyDictionary<ArmorAttachmentType, ArmorStatus> GetAllArmorStats();

        /// <summary> ボスの防御力のステータスを取得する </summary>
        /// <param name="damageType"> ボスの防御力の種類 </param>
        public int GetBodyDefense(TakeDamageType damageType);

        /// <summary> タイムスケールを設定 </summary>
        /// <param name="timeScale"> 新しいタイムスケール </param>
        public void SetTimeScale(float timeScale);

        /// <summary> 攻撃の標的を設定する </summary>
        /// <param name="nextTarget"> 次の攻撃の標的 </param>
        public void SetAttackTarget(IPlayer nextTarget);

        /// <summary> 実行する攻撃を選択肢から選択する </summary>
        public void SelectNextAttackData(int selectPoolID);

        /// <summary> 攻撃を実行する </summary>
        public UniTask ExecuteAttack();

        /// <summary> 攻撃の当たり判定を行う </summary>
        /// <param name="attackHitAreaType"> 当たり判定の形 </param>
        /// <param name="attackPosition"> 当たり判定の中心座標 </param>
        /// <param name="forward"> 必要であれば当たり判定を行う方角を渡す </param>
        public void CheckHitAttack(AttackHitAreaType attackHitAreaType, Vector3 attackPosition, Vector3 forward = default);

        /// <summary> 攻撃を終了する </summary>
        public void AttackEnd();

        /// <summary> 現在のキャラクターの姿勢を変更 </summary>
        /// <param name="postureType"> 変更後の姿勢 </param>
        public void SetCharacterPosture(PostureType postureType);

        /// <summary> BossEnemyへのダメージ処理 </summary>
        /// <param name="damage"> ダメージの総量 </param>
        /// <param name="scapegoatArmor"> 本体の代わりにダメージを背負う鎧 </param>
        public void TakeDamage(int damage, ArmorAttachmentType scapegoatArmor = ArmorAttachmentType.None);

        /// <summary> 鎧の修復処理 </summary>
        /// <param name="repairArmor"> 特定の修復ヶ所(特に指定がなければすべて修復する) </param>
        /// <param name="repairedArmorHP"> 修復後の鎧のHP(特に指定がなければ最大値になる) </param>
        public void RepairArmor(ArmorAttachmentType repairArmor = ArmorAttachmentType.None, int repairedArmorHP = 0);

        /// <summary> フェーズ切り替え処理 </summary>
        public void OnPhaseChange();

        /// <summary> 死亡時の処理 </summary>
        public void HandleDead();
    }
    #endregion

    /// <summary> BossEnemyのEntity </summary>
    public class BossCharacterEntity : IBossCharacterEntity
    {
        public event Action OnArmorBreak;

        public event Action OnDead;

        public BossCharacterEntity(string name, CharacterStatus[] characterStatus)
        {
            _bossName = name;
            _allPhaseStats = characterStatus;
        }

        /// <summary> ボスの名前 </summary>
        public string BossName => _bossName;

        /// <summary> 攻撃標的 </summary>
        public IPlayer AttackTarget => _attackTarget;

        /// <summary> 実行中の攻撃データ </summary>
        public Attack.AttackData ExecutingAttackData 
        {
            get
            {
                if (!_isAttacking.Value) return default;

                return _attackExecutor.ExecutingAttack;
            }
        }

        /// <summary> 攻撃の選択肢のID </summary>
        public int CurrentAttackSelectPoolID => _attackSelectPoolID;

        /// <summary> 現在のHP </summary>
        public IReadOnlyReactiveProperty<int> CurrentHP => _currentHP;

        /// <summary> 現在座標 </summary>
        public IReadOnlyReactiveProperty<Vector3> Position => _position;

        /// <summary> 回転情報 </summary>
        public IReadOnlyReactiveProperty<Quaternion> Rotation => _rotation;

        /// <summary> 移動速度 </summary>
        public IReadOnlyReactiveProperty<Vector3> Velocity => _velocity;

        /// <summary> 現在の姿勢 </summary>
        public IReadOnlyReactiveProperty<PostureType> CurrentCharacterPostureType => _currentCharacterPostureType;

        /// <summary> Phase切り替え中フラグ </summary>
        public IReadOnlyReactiveProperty<bool> IsPhaseChaging => _isPhaseChanging;

        /// <summary> 攻撃中フラグ </summary>
        public IReadOnlyReactiveProperty<bool> IsAttacking => _isAttacking;

        /// <summary> ボスのタイムスケール </summary>
        public float TimeScale => _timeScale;

        /// <summary> キャラクターの現在のステータス </summary>
        public CharacterStatus CharacterCurrentStats => _currentPhaseStats;

        /// <summary> 全Phaseのキャラクターステータス </summary>
        public CharacterStatus[] AllPhaseCharacterStats => _allPhaseStats;

        /// <summary> 各種鎧の現在のHP状況 </summary>
        public IReadOnlyDictionary<ArmorAttachmentType, int> ArmorCurrentHPDict => _armorCurrentHPDict;

        /// <summary> 初期化 </summary>
        public void Init()
        {
            // 現在のフェーズを最初のフェーズにする
            _currentPhaseNum = 0;

            // ReactivePropertyの初期化
            _isPhaseChanging = new(false);
            _isAttacking = new(false);
            _currentHP = new();
            _position = new();
            _rotation = new();
            _velocity = new();

            // タイムスケールを初期化
            _timeScale = 1.0f;

            // 攻撃実行クラスを初期化
            _attackExecutor = new();
        }

        /// <summary> 生成(スポーン)された際の処理 </summary>
        public void Spawn(IPlayer firstTarget, Transform characterTransform)
        {
            _attackTarget = firstTarget;
            SetPosition(characterTransform.position);
            SetRotation(characterTransform.rotation);
            SetVelocity(Vector3.zero);

            PhaseChange();
        }

        /// <summary> 装備中の鎧のステータスを取得する </summary>
        /// <param name="armorAttachmentType"> 取得したい鎧の種類 </param>
        public ArmorStatus GetArmorStats(ArmorAttachmentType armorAttachmentType)
        {
            // 取得したい鎧がDictionary内に存在すればその値を返す
            if (_currentPhaseStats.AttachmentArmorStatsDict.ContainsKey(armorAttachmentType))
                return _currentPhaseStats.AttachmentArmorStatsDict[armorAttachmentType]; 

            // もし取得したい鎧がDictionary内に存在しなければエラーログを出してデフォルト値を返す
            Debug.LogError($"対象の鎧の取得に失敗しました : 取得対象< { armorAttachmentType } >");
            return default;
        }

        /// <summary> 装備中の全ての鎧のステータスを取得する </summary>
        public IReadOnlyDictionary<ArmorAttachmentType, ArmorStatus> GetAllArmorStats()
        {
            return _currentPhaseStats.AttachmentArmorStatsDict;
        }

        /// <summary> ボスの防御力のステータスを取得する </summary>
        /// <param name="damageType"> ボスの防御力の種類 </param>
        public int GetBodyDefense(TakeDamageType damageType)
        {
            // 取得したい部位の防御力がDictionary内に存在すればその値を返す
            if (_currentPhaseStats.BodyPartsDefenseDict.ContainsKey(damageType))
                return _currentPhaseStats.BodyPartsDefenseDict[damageType];

            // もし取得したい部位の防御力がDictionary内に存在しなければエラーログを出してデフォルト値を返す
            Debug.LogError($"対象の防御力の取得に失敗しました : 取得対象< { damageType } >");
            return default;
        }

        /// <summary> BossEnemyの座標を設定する </summary>
        /// <param name="position"> 新しい座標 </param>
        public void SetPosition(Vector3 position) => _position.Value = position;

        /// <summary> BossEnemyの回転を設定する </summary>
        /// <param name="rotation"> 新しい回転 </param>
        public void SetRotation(Quaternion rotation) => _rotation.Value = rotation;

        /// <summary> BossEnemyの移動速度を設定する </summary>
        /// <param name="velocity"> 移動速度 </param>
        public void SetVelocity(Vector3 velocity) => _velocity.Value = velocity;

        /// <summary> タイムスケールを設定 </summary>
        /// <param name="timeScale"> 新しいタイムスケール </param>
        public void SetTimeScale(float timeScale) => _timeScale = timeScale;

        /// <summary> 攻撃の標的を設定する </summary>
        /// <param name="nextTarget"> 次の攻撃の標的 </param>
        public void SetAttackTarget(IPlayer nextTarget) => _attackTarget = nextTarget;

        /// <summary> 実行する攻撃を選択肢から選択する </summary>
        public void SelectNextAttackData(int selectPoolID)
        {
            _attackExecutor.SetExecuteAttack(selectPoolID);
        }

        /// <summary> 攻撃実行処理 </summary>
        public async UniTask ExecuteAttack()
        {
            if(AttackTarget == null) return;

            _attackExecutor.Execute(_attackTarget);

            _isAttacking.Value = true;
        }

        /// <summary> 攻撃の当たり判定を行う </summary>
        /// <param name="attackHitAreaType"> 当たり判定の形 </param>
        /// <param name="attackPosition"> 当たり判定の中心座標 </param>
        /// <param name="forward"> 必要であれば当たり判定を行う方角を渡す </param>
        public void CheckHitAttack(AttackHitAreaType attackHitAreaType, Vector3 attackPosition, Vector3 forward = default)
        {
            if (!_isAttacking.Value) return;


        }

        /// <summary> 攻撃終了処理 </summary>
        public void AttackEnd()
        {
            if (!_isAttacking.Value) return;

            _attackExecutor.Complete();
            _isAttacking.Value = false;
        }

        /// <summary> 現在のキャラクターの姿勢を変更 </summary>
        /// <param name="postureType"> 変更後の姿勢 </param>
        public void SetCharacterPosture(PostureType postureType)
        {
            if(_currentCharacterPostureType.Value == postureType) return;

            _currentCharacterPostureType.Value = postureType;
        }

        /// <summary> BossEnemyへのダメージ処理 </summary>
        /// <param name="damage"> ダメージの総量 </param>
        /// <param name="scapegoatArmor"> 本体の代わりにダメージを背負う鎧 </param>
        public void TakeDamage(int damage, ArmorAttachmentType scapegoatArmor = ArmorAttachmentType.None)
        {
            if (scapegoatArmor == ArmorAttachmentType.None)
            {
                _currentHP.Value -= damage;

                if (_currentHP.Value <= 0)
                {
                    _currentHP.Value = 0;
                    PhaseChange();
                }

                return;
            }

            foreach (var armorAttachmentType in _armorCurrentHPDict.Keys)
            {
                if (armorAttachmentType != scapegoatArmor) continue;

                if (_armorCurrentHPDict[armorAttachmentType] - damage <= 0)
                {
                    _armorCurrentHPDict[armorAttachmentType] = 0;
                    GetArmorStats(armorAttachmentType).Break();
                    OnArmorBreak?.Invoke();

                    return;
                }

                _armorCurrentHPDict[armorAttachmentType] -= damage;
            }
        }

        /// <summary> 鎧の修復処理 </summary>
        /// <param name="repairArmor"> 特定の修復ヶ所(特に指定がなければすべて修復する) </param>
        /// <param name="repairedArmorHP"> 修復後の鎧のHP(特に指定がなければ最大値になる) </param>
        public void RepairArmor(ArmorAttachmentType repairArmor = ArmorAttachmentType.None, int repairedArmorHP = 0)
        {
            _currentPhaseStats.RepairArmor(repairArmor, repairedArmorHP);
        }

        /// <summary> フェーズ切り替え処理 </summary>
        public void OnPhaseChange()
        {
            _isPhaseChanging.Value = true;
        }

        /// <summary> フェーズ切り替え終了時の処理 </summary>
        public void OnPhaseChanged()
        {
            _isPhaseChanging.Value = false;
        }

        /// <summary> 死亡時のイベント発火 </summary>
        public void HandleDead() => OnDead?.Invoke();

        // 名前
        private string _bossName;

        // ボスの攻撃の標的
        private IPlayer _attackTarget;

        // 実行中の攻撃データID
        private int _attackSelectPoolID;

        // BossEnemyの現在のHP
        private ReactiveProperty<int> _currentHP;

        // 現在座標
        private ReactiveProperty<Vector3> _position;

        // 回転座標
        private ReactiveProperty<Quaternion> _rotation;

        // 移動速度
        private ReactiveProperty<Vector3> _velocity;

        // キャラクターの姿勢
        private ReactiveProperty<PostureType> _currentCharacterPostureType = new(PostureType.Standing);

        // Phase切り替え中フラグ
        private ReactiveProperty<bool> _isPhaseChanging;

        // 攻撃中フラグ
        private ReactiveProperty<bool> _isAttacking;

        // ボスのタイムスケール
        private float _timeScale;

        // 現在のステータス
        private CharacterStatus _currentPhaseStats;

        // 各フェーズごとのステータス
        private CharacterStatus[] _allPhaseStats;

        private Dictionary<ArmorAttachmentType, int> _armorCurrentHPDict;

        // 現在のフェーズ
        private int _currentPhaseNum = 0;

        // 攻撃実行クラス
        private Attack.AttackExecutor _attackExecutor;

        /// <summary> 現在のPhaseから次のPhaseに移行する処理 </summary>
        private void PhaseChange()
        {
            // 全てのPhaseが終了していたら死亡する
            if (_allPhaseStats.Length <= _currentPhaseNum)
            {
                HandleDead();
                return;
            }

            // 次のPhaseに移行
            _currentPhaseStats = _allPhaseStats[_currentPhaseNum];
            _currentPhaseNum++;

            // 現在のHPをつぎのPhaseのMaxHPにする
            _currentHP.Value = _currentPhaseStats.MaxHP;

            _armorCurrentHPDict = new();
            foreach (var key in GetAllArmorStats().Keys)
            {
                _armorCurrentHPDict.Add(key, GetArmorStats(key).MaxHP);
            }

            // フェーズ切り替えフラグをTrueにする
            _isPhaseChanging.Value = true;
        }
    }

    #region ボスエネミー本体のステータス
    [Serializable]
    public struct CharacterStatus
    {
        public CharacterStatus(int phaseNum, int maxHP, float walkSpeed, 
            Dictionary<TakeDamageType, int> bodyPartsDefenseDict,
            Dictionary<ArmorAttachmentType, ArmorStatus> attachmentArmorStatsDict)
        {
            _phaseNum = phaseNum;
            _maxHP = maxHP;
            _walkSpeed = walkSpeed;
            _bodyPartsDefenseDict = bodyPartsDefenseDict;
            _attachmentArmorStatsDict = attachmentArmorStatsDict;
        }

        /// <summary> 現在のPhase </summary>
        public int PhaseNum => _phaseNum;

        /// <summary> 最大HP </summary>
        public int MaxHP => _maxHP;

        /// <summary> 歩行速度 </summary>
        public float WalkSpeed => _walkSpeed;

        /// <summary> ボスの体の各部位の防御力を持つDictionary </summary>
        public IReadOnlyDictionary<TakeDamageType, int> BodyPartsDefenseDict => _bodyPartsDefenseDict;

        /// <summary> ボスが装着している各部鎧のステータス収納Dictionary </summary>
        public IReadOnlyDictionary<ArmorAttachmentType, ArmorStatus> AttachmentArmorStatsDict => _attachmentArmorStatsDict;

        /// <summary> 初期化 </summary>
        public void Init()
        {
            foreach(var attachmentArmorType in _attachmentArmorStatsDict.Keys)
            {
                var newArmorStats = _attachmentArmorStatsDict[attachmentArmorType];

                newArmorStats.Init();

                _attachmentArmorStatsDict[attachmentArmorType] = newArmorStats;
            }
        }

        /// <summary> 鎧の修復処理 </summary>
        /// <param name="repairArmor"> 特定の修復ヶ所(特に指定がなければすべて修復する) </param>
        /// <param name="repairedArmorHP"> 修復後の鎧のHP(特に指定がなければ最大値になる) </param>
        public void RepairArmor(ArmorAttachmentType repairArmor, int repairedArmorHP)
        {
            ArmorStatus targetStats;

            // 特に指定がなければ(repairArmorがArmorAttachmentType.Noneなら)すべて修復する
            if (repairArmor == ArmorAttachmentType.None)
            {
                foreach(var attachmentType in _attachmentArmorStatsDict.Keys)
                {
                    // 対象の鎧を取得
                    targetStats = _attachmentArmorStatsDict[attachmentType];

                    // 対象の鎧を修復
                    targetStats.Repair();
                    _attachmentArmorStatsDict[attachmentType] = targetStats;
                }

                return;
            }

            // 指定された部分の鎧を修復する

            // 対象の鎧を取得
            targetStats = _attachmentArmorStatsDict[repairArmor];

            // 対象の鎧を修復
            targetStats.Repair();
            _attachmentArmorStatsDict[repairArmor] = targetStats;
        }

        // 現在のPhase
        private int _phaseNum;

        // 最大HP
        private int _maxHP;

        // 歩行速度
        private float _walkSpeed;

        // ボスの各部位の防御力
        private Dictionary<TakeDamageType, int> _bodyPartsDefenseDict;

        // ボスが装着している各部鎧のステータス
        private Dictionary<ArmorAttachmentType, ArmorStatus> _attachmentArmorStatsDict;
    }
    #endregion
}
