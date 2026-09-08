using BossEnemy.Armor;
using BossEnemy.Enum;
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
        /// <summary> 鎧破壊時のイベント </summary>
        public event Action<ArmorAttachmentType> OnArmorBreak;

        /// <summary> 鎧修復時のイベント </summary>
        public event Action<ArmorAttachmentType> OnArmorRepair;

        /// <summary> ボスの攻撃命中時イベント </summary>
        public event Action OnAttackHit;

        /// <summary> 死亡時のイベント </summary>
        public event Action OnDead;

        /// <summary> ボスの名前 </summary>
        public string BossName { get; }

        /// <summary> 攻撃標的 </summary>
        public IPlayer AttackTarget { get; }

        /// <summary> 現在のHP </summary>
        public IReadOnlyReactiveProperty<int> CurrentHP { get; }

        /// <summary> 現在の姿勢 </summary>
        public IReadOnlyReactiveProperty<PostureType> CurrentCharacterPostureType { get; }

        /// <summary> Phase切り替え中フラグ </summary>
        public IReadOnlyReactiveProperty<bool> IsPhaseChaging { get; }

        /// <summary> 攻撃中フラグ </summary>
        public IReadOnlyReactiveProperty<Attack.AttackData> ExecutingAttackData { get; }

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
        public void OnSpawn(IPlayer firstTarget, Vector3 position, Quaternion quaternion);

        /// <summary> 装備中の鎧のステータスを取得する </summary>
        /// <param name="armorAttachmentType"> 取得したい鎧の種類 </param>
        public ArmorStatus GetArmorStats(ArmorAttachmentType armorAttachmentType);

        /// <summary> 装備中の全ての鎧のステータスを取得する </summary>
        public IReadOnlyDictionary<ArmorAttachmentType, ArmorStatus> GetAllArmorStats();

        /// <summary> ボスの防御力のステータスを取得する </summary>
        /// <param name="damageType"> ボスの防御力の種類 </param>
        public int GetBodyDefense(TakeDamageType damageType);

        /// <summary> 発動予定の攻撃を取得する </summary>
        public Attack.AttackData GetNextAttackData();

        /// <summary> タイムスケールを設定 </summary>
        /// <param name="timeScale"> 新しいタイムスケール </param>
        public void SetTimeScale(float timeScale);

        /// <summary> 攻撃の標的を設定する </summary>
        /// <param name="nextTarget"> 次の攻撃の標的 </param>
        public void SetAttackTarget(IPlayer nextTarget);

        /// <summary> 現在のキャラクターの姿勢を変更 </summary>
        /// <param name="postureType"> 変更後の姿勢 </param>
        public void SetCharacterPosture(PostureType postureType);

        /// <summary> 実行する攻撃を選択肢から選択する </summary>
        public UniTask SelectNextAttackData(int selectPoolID);

        /// <summary> 攻撃を実行する </summary>
        public UniTask ExecuteAttack();

        /// <summary> 攻撃の当たり判定を行う </summary>
        /// <param name="attackHitAreaType"> 当たり判定の形 </param>
        /// <param name="attackPosition"> 当たり判定の中心座標 </param>
        /// <param name="forward"> 必要であれば当たり判定を行う方角を渡す </param>
        public void TryHitAttackDamageToTarget(AttackHitAreaType attackHitAreaType, Vector3 attackPosition, Vector3 forward = default);

        /// <summary> 攻撃を終了する </summary>
        public void AttackCompleted();

        /// <summary> BossEnemyへのダメージ処理 </summary>
        /// <param name="damage"> ダメージの総量 </param>
        /// <param name="scapegoatArmor"> 本体の代わりにダメージを背負う鎧 </param>
        public void TakeDamage(int damage, ArmorAttachmentType scapegoatArmor = ArmorAttachmentType.None);

        /// <summary> 鎧の修復処理 </summary>
        /// <param name="repairArmor"> 特定の修復ヶ所(特に指定がなければすべて修復する) </param>
        /// <param name="repairedArmorHP"> 修復後の鎧のHP(特に指定がなければ最大値になる) </param>
        public void RepairArmor(ArmorAttachmentType repairArmor = ArmorAttachmentType.None, int repairedArmorHP = 0);

        /// <summary> 現在のPhaseから次のPhaseに移行する処理 </summary>
        public void PhaseChange();

        /// <summary> フェーズ切り替え終了時の処理 </summary>
        public void PhaseChangeCompleted();

        /// <summary> 死亡時の処理 </summary>
        public void HandleDead();
    }
    #endregion

    /// <summary> BossEnemyのEntity </summary>
    public class BossCharacterEntity : IBossCharacterEntity
    {
        public event Action<ArmorAttachmentType> OnArmorBreak;

        public event Action<ArmorAttachmentType> OnArmorRepair;

        public event Action OnAttackHit;

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

        /// <summary> 現在のHP </summary>
        public IReadOnlyReactiveProperty<int> CurrentHP => _currentHP;

        /// <summary> 現在座標 </summary>
        public IReadOnlyReactiveProperty<Vector3> Position => _position;

        /// <summary> 回転情報 </summary>
        public IReadOnlyReactiveProperty<Quaternion> Rotation => _rotation;

        /// <summary> 移動速度 </summary>
        public IReadOnlyReactiveProperty<Vector3> Velocity => _velocity;

        /// <summary> 現在の姿勢 </summary>
        public IReadOnlyReactiveProperty<PostureType> CurrentCharacterPostureType => _currentPostureType;

        /// <summary> Phase切り替え中フラグ </summary>
        public IReadOnlyReactiveProperty<bool> IsPhaseChaging => _isPhaseChanging;

        /// <summary> 攻撃中フラグ </summary>
        public IReadOnlyReactiveProperty<Attack.AttackData> ExecutingAttackData => _executingAttack;

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
            // 攻撃実行クラスを初期化
            _attackExecutor = new();

            // 現在のフェーズを最初のフェーズにする
            _currentPhaseNum = 0;

            // ReactivePropertyの初期化
            _currentPostureType = new(PostureType.Standing);
            _isPhaseChanging = new(false);
            _executingAttack = new(default);
            _currentHP = new();
            _position = new();
            _rotation = new();
            _velocity = new();

            // タイムスケールを初期化
            _timeScale = 1.0f;
        }

        /// <summary> 生成(スポーン)された際の処理 </summary>
        public void OnSpawn(IPlayer firstTarget, Vector3 position, Quaternion quaternion)
        {
            _attackTarget = firstTarget;
            SetPosition(position);
            SetRotation(quaternion);
            SetVelocity(Vector3.zero);

            PhaseChange();

            Debug.Log("召喚されました");
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

        /// <summary> 発動予定の攻撃を取得する </summary>
        public Attack.AttackData GetNextAttackData()
        {
            return _attackExecutor.ExecutingAttack;
        }

        /// <summary> 実行する攻撃を選択肢から選択する </summary>
        public async UniTask SelectNextAttackData(int selectPoolID)
        {
            await _attackExecutor.SetNextAttack(selectPoolID);
            return;
        }

        /// <summary> 攻撃実行処理 </summary>
        public async UniTask ExecuteAttack()
        {
            if(AttackTarget == null) return;

            _attackExecutor.Execute(_attackTarget);

            _executingAttack.Value = _attackExecutor.ExecutingAttack;
        }

        /// <summary> 攻撃の当たり判定を行う </summary>
        /// <param name="attackHitAreaType"> 当たり判定の形 </param>
        /// <param name="attackPosition"> 当たり判定の中心座標 </param>
        /// <param name="forward"> 必要であれば当たり判定を行う方角を渡す </param>
        public void TryHitAttackDamageToTarget(AttackHitAreaType attackHitAreaType, Vector3 attackPosition, Vector3 forward = default)
        {
            if (_executingAttack == null) return;

            if(_attackExecutor.TryHitAttack(attackHitAreaType, attackPosition, forward))
            {
                OnAttackHit?.Invoke();
            }
        }

        /// <summary> 攻撃終了処理 </summary>
        public void AttackCompleted()
        {
            if (_executingAttack == null) return;

            _attackExecutor.Complete();
            _executingAttack.Value = default;
        }

        /// <summary> 現在のキャラクターの姿勢を変更 </summary>
        /// <param name="postureType"> 変更後の姿勢 </param>
        public void SetCharacterPosture(PostureType postureType)
        {
            if(_currentPostureType.Value == postureType) return;

            _currentPostureType.Value = postureType;
        }

        /// <summary> BossEnemyへのダメージ処理 </summary>
        /// <param name="damage"> ダメージの総量 </param>
        /// <param name="scapegoatArmor"> 本体の代わりにダメージを背負う鎧 </param>
        public void TakeDamage(int damage, ArmorAttachmentType scapegoatArmor = ArmorAttachmentType.None)
        {
            // 攻撃を身代わりしてくれる鎧がなければ自身のＨＰを減少する
            if (scapegoatArmor != ArmorAttachmentType.None)
            {
                if (TryTakeDamageArmor(scapegoatArmor, damage)) return;
            }

            if (_currentHP.Value - damage < 0)
            {
                _currentHP.Value = 0;
                return;
            }

            _currentHP.Value -= damage;
        }

        /// <summary> 鎧の修復処理 </summary>
        /// <param name="repairArmor"> 特定の修復ヶ所(特に指定がなければすべて修復する) </param>
        /// <param name="repairedArmorHP"> 修復後の鎧のHP(特に指定がなければ最大値になる) </param>
        public void RepairArmor(ArmorAttachmentType repairArmor = ArmorAttachmentType.None, int repairedArmorHP = 0)
        {
            if(repairArmor != ArmorAttachmentType.None)
            {
                if (!_armorCurrentHPDict.ContainsKey(repairArmor))
                    _armorCurrentHPDict.Add(repairArmor, GetArmorStats(repairArmor).MaxHP);
                else
                    _armorCurrentHPDict[repairArmor] = GetArmorStats(repairArmor).MaxHP;

                _currentPhaseStats.RepairArmor(repairArmor);
                // スポーン初期化中など、購読者がまだ登録されていない場合がある。
                OnArmorRepair?.Invoke(repairArmor); 
                return;
            }

            // RepairArmor がステータスDictionaryの値を書き換えるため、
            // Keys を直接列挙すると Dictionary の列挙バージョンが変わる。
            var armorTypes = new List<ArmorAttachmentType>(GetAllArmorStats().Keys);
            foreach (var key in armorTypes)
            {
                if (!_armorCurrentHPDict.ContainsKey(key))
                    _armorCurrentHPDict.Add(key, GetArmorStats(key).MaxHP);
                else
                    _armorCurrentHPDict[key] = GetArmorStats(key).MaxHP;

                _currentPhaseStats.RepairArmor(key);
            }
            // スポーン初期化中など、購読者がまだ登録されていない場合がある。
            OnArmorRepair?.Invoke(ArmorAttachmentType.None);
        }

        /// <summary> 現在のPhaseから次のPhaseに移行する処理 </summary>
        public void PhaseChange()
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

            // 装備中のアーマーの初期化
            _armorCurrentHPDict = new();
            foreach (var key in GetAllArmorStats().Keys)
            {
                _armorCurrentHPDict.Add(key, GetArmorStats(key).MaxHP);
            }
            RepairArmor();

            // フェーズ切り替えフラグをTrueにする
            _isPhaseChanging.Value = true;
        }

        /// <summary> フェーズ切り替え終了時の処理 </summary>
        public void PhaseChangeCompleted()
        {
            _isPhaseChanging.Value = false;
        }

        /// <summary> 死亡時のイベント発火 </summary>
        public void HandleDead() => OnDead?.Invoke();

        // 名前
        private string _bossName;

        // ボスの攻撃の標的
        private IPlayer _attackTarget;

        // BossEnemyの現在のHP
        private ReactiveProperty<int> _currentHP = null;

        // 現在座標
        private ReactiveProperty<Vector3> _position = null;

        // 回転座標
        private ReactiveProperty<Quaternion> _rotation = null;

        // 移動速度
        private ReactiveProperty<Vector3> _velocity = null;

        // キャラクターの姿勢
        private ReactiveProperty<PostureType> _currentPostureType = null;

        // Phase切り替え中フラグ
        private ReactiveProperty<bool> _isPhaseChanging = null;

        // 攻撃中フラグ
        private ReactiveProperty<Attack.AttackData> _executingAttack = null;

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

        private bool TryTakeDamageArmor(ArmorAttachmentType scapegoatArmor, int damage)
        {
            if (!_armorCurrentHPDict.ContainsKey(scapegoatArmor))
            {
                Debug.LogError("攻撃対象の鎧の鎧が見つかりません");
                return false;
            }

            if (GetArmorStats(scapegoatArmor).IsArmorBroken) return false;

            if (_armorCurrentHPDict[scapegoatArmor] - damage <= 0)
            {
                _armorCurrentHPDict[scapegoatArmor] = 0;
                _currentPhaseStats.BreakArmor(scapegoatArmor);
                OnArmorBreak?.Invoke(scapegoatArmor);
                Debug.Log($"鎧が破壊されました 破壊箇所: {scapegoatArmor} ");

                return true;
            }

            _armorCurrentHPDict[scapegoatArmor] -= damage;
            Debug.Log($"鎧が攻撃を受けました 攻撃箇所: {scapegoatArmor} 残りのHP:{_armorCurrentHPDict[scapegoatArmor]}");
            return true;
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
            // Dictionary の値更新中に Keys を列挙しないよう、キーを退避する。
            var attachmentArmorTypes = new List<ArmorAttachmentType>(_attachmentArmorStatsDict.Keys);
            foreach (var attachmentArmorType in attachmentArmorTypes)
            {
                var newArmorStats = _attachmentArmorStatsDict[attachmentArmorType];

                newArmorStats.Init();

                _attachmentArmorStatsDict[attachmentArmorType] = newArmorStats;
            }
        }

        /// <summary> 鎧の破壊処理 </summary>
        public void BreakArmor(ArmorAttachmentType breakArmor)
        {
            ArmorStatus targetStats = _attachmentArmorStatsDict[breakArmor];

            targetStats.Break();
            _attachmentArmorStatsDict[breakArmor] = targetStats;
        }

        /// <summary> 鎧の修復処理 </summary>
        /// <param name="repairArmor"> 特定の修復ヶ所(特に指定がなければすべて修復する) </param>
        /// <param name="repairedArmorHP"> 修復後の鎧のHP(特に指定がなければ最大値になる) </param>
        public void RepairArmor(ArmorAttachmentType repairArmor)
        {
            ArmorStatus targetStats;

            // 特に指定がなければ(repairArmorがArmorAttachmentType.Noneなら)すべて修復する
            if (repairArmor == ArmorAttachmentType.None)
            {
                // Repair による値更新で列挙子を無効化しないよう、キーを退避する。
                var attachmentTypes = new List<ArmorAttachmentType>(_attachmentArmorStatsDict.Keys);
                foreach (var attachmentType in attachmentTypes)
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
