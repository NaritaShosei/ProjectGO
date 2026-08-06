using BossEnemy.Armor;
using BossEnemy.Enum;
using System;
using System.Collections.Generic;
using UniRx;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

// BossEnemyに関するData
namespace BossEnemy.Character
{
    /// <summary> BossEnemyのEntity </summary>
    public class BossCharacterEntity : IMoveTarget
    {
        /// <summary> ボスの名前 </summary>
        public string BossName => _bossName;

        /// <summary> 歩行速度 </summary>
        public float WalkSpeed => _walkSpeed;

        /// <summary> 現在座標 </summary>
        public IReadOnlyReactiveProperty<Vector3> Position => _position;

        /// <summary> 回転情報 </summary>
        public IReadOnlyReactiveProperty<Quaternion> Rotation => _rotation;

        /// <summary> 移動速度 </summary>
        public IReadOnlyReactiveProperty<Vector3> Velocity => _velocity;

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

        /// <summary> BossEnemyへのダメージ処理 </summary>
        /// <param name="damage"> ダメージの総量 </param>
        /// <param name="scapegoatArmor"> 本体の代わりにダメージを背負う鎧 </param>
        public void TakeDamage(int damage, ArmorAttachmentType scapegoatArmor = ArmorAttachmentType.None)
        {
            _currentPhaseStats.TakeDamage(damage, scapegoatArmor);
        }

        /// <summary> 鎧の修復処理 </summary>
        /// <param name="repairArmor"> 特定の修復ヶ所(特に指定がなければすべて修復する) </param>
        /// <param name="repairedArmorHP"> 修復後の鎧のHP(特に指定がなければ最大値になる) </param>
        public void RepairArmor(ArmorAttachmentType repairArmor = ArmorAttachmentType.None, int repairedArmorHP = 0)
        {
            _currentPhaseStats.RepairArmor(repairArmor, repairedArmorHP);
        }

        // 名前
        private string _bossName;

        // 歩行速度
        private float _walkSpeed;

        // 現在座標
        private ReactiveProperty<Vector3> _position;

        // 回転座標
        private ReactiveProperty<Quaternion> _rotation;

        // 移動速度
        private ReactiveProperty<Vector3> _velocity;

        // 現在のステータス
        private CharacterStatus _currentPhaseStats;

        // 各フェーズごとのステータス
        private CharacterStatus[] _allPhaseStats;
    }

    #region ボスエネミー本体のステータス
    [Serializable]
    public struct CharacterStatus
    {
        /// <summary> 現在のHP </summary>
        public IReadOnlyReactiveProperty<int> CurrentHP => _currentHP;
        
        /// <summary> 最大HP </summary>
        public int MaxHP => _maxHP;

        /// <summary> ボスの体の各部位の防御力を持つDictionary </summary>
        public IReadOnlyDictionary<TakeDamageType, int> BodyPartsDefenseDict => _bodyPartsDefenseDict;

        /// <summary> ボスが装着している各部鎧のステータス収納Dictionary </summary>
        public IReadOnlyDictionary<ArmorAttachmentType, ArmorStatus> AttachmentArmorStatsDict => _attachmentArmorStatsDict;

        /// <summary> BossEnemyへのダメージ </summary>
        /// <param name="damage"> ダメージの総量 </param>
        /// <param name="scapegoatArmor"> 本体の代わりにダメージを背負う鎧 </param>
        public void TakeDamage(int damage, ArmorAttachmentType scapegoatArmor)
        {
            // もし身代わりとなる鎧があれば鎧へのダメージ処理を行う
            if(scapegoatArmor == ArmorAttachmentType.None)
            {
                // 対象の鎧を取得
                var targetStatus = _attachmentArmorStatsDict[scapegoatArmor];

                // 対象の鎧へダメージを与える
                targetStatus.TakeDamage(damage);
                _attachmentArmorStatsDict[scapegoatArmor] = targetStatus;
                return;
            }

            // ダメージを受けてHPが0未満になればHPを強制的に0にする
            if (_currentHP.Value - damage <= 0)
                _currentHP.Value = 0;

            // 1以上ならダメージ分を本体のHPから引く
            else _currentHP.Value -= damage;
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
                    targetStats.Repair(repairedArmorHP);
                    _attachmentArmorStatsDict[attachmentType] = targetStats;
                }

                return;
            }

            // 指定された部分の鎧を修復する

            // 対象の鎧を取得
            targetStats = _attachmentArmorStatsDict[repairArmor];

            // 対象の鎧を修復
            targetStats.Repair(repairedArmorHP);
            _attachmentArmorStatsDict[repairArmor] = targetStats;
        }

        // 最大HP
        private int _maxHP;

        // BossEnemyの現在のHP
        private ReactiveProperty<int> _currentHP;

        // ボスの各部位の防御力
        private Dictionary<TakeDamageType, int> _bodyPartsDefenseDict;

        // ボスが装着している各部鎧のステータス
        private Dictionary<ArmorAttachmentType, ArmorStatus> _attachmentArmorStatsDict;
    }
    #endregion
}
