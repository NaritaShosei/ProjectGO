using BossEnemy.BehaviorTree;
using System;
using UnityEngine;
using UniRx;

// BossEnemyに関するData
namespace BossEnemy.Data
{
    /// <summary> BossEnemyのData </summary>
    [CreateAssetMenu(fileName = "BossEnemyData", menuName = "BossEnemy/BossEnemyData")]
    public class BossEnemyData : ScriptableObject
    {
        /// <summary> ボスエネミーを構成する各パーツの種類 </summary>
        public enum BossEnemyPartsType
        {
            None,
            Head,
            Body,
            Arm,
            Leg
        }

        /// <summary> 現在のHP </summary>
        public IReadOnlyReactiveProperty<int> CurrentHP => _currentHP;

        /// <summary> BossEnemyの座標データ </summary>
        public IReadOnlyReactiveProperty<Vector3> Position => _position;

        /// <summary> BossEnemyの回転データ </summary>
        public IReadOnlyReactiveProperty<Quaternion> Rotation => _rotation;

        /// <summary> 最大HP </summary>
        public int MaxHP => _maxHP;

        /// <summary> ボス自身の攻撃力 </summary>
        public int AttackPower => _attackPower;

        /// <summary> 頭の防御力 </summary>
        public int HeadDefense => _currentHeadDefense;

        /// <summary> 胴体の防御力 </summary>
        public int BodyDefense => _currentBodyDefense;

        /// <summary> 腕の防御力 </summary>
        public int ArmDefense => _currentaArmDefense;

        /// <summary> 足の防御力 </summary>
        public int LegDefense => _currentLegDefense;

        /// <summary> ボスが装着する右手ArmerのData </summary>
        public BossArmerData RightArmArmer => _rightArmArmer;

        /// <summary> ボスが装着する左手ArmerのData </summary>
        public BossArmerData LeftArmArmer => _leftArmArmer;

        /// <summary> ボスが装着する右足ArmerのData</summary>
        public BossArmerData RightLegArmer => _rightLegArmer;

        /// <summary> ボスが装着する左足ArmerのData </summary>
        public BossArmerData LeftLegArmer => _leftLegArmer;

        /// <summary> このEnemyを制御するBehaiviorTreeのEntryNode </summary>
        public ITreeNode OriginNode => _originNode;

        /// <summary> BossEnemyの初期化メソッド </summary>
        public void Init(Transform bossEnemyTransform)
        {
            // 現在の座標をセット
            SetPosition(bossEnemyTransform.position);
            // 現在の回転座標をセット
            SetRotation(bossEnemyTransform.rotation);

            // HPを最大値にする
            _currentHP.Value = _maxHP;

            // 各アーマーの初期化
            _rightArmArmer.Init();
            _leftArmArmer.Init();
            _rightLegArmer.Init();
            _leftLegArmer.Init();

            // 各パーツの防御力を初期化
            _currentHeadDefense = _headDefense;
            _currentBodyDefense = _bodyDefense;
            _currentaArmDefense = _armDefense;
            _currentLegDefense = _legDefense;
        }

        /// <summary> BossEnemyの座標を設定する </summary>
        /// <param name="position"> 新しい座標 </param>
        public void SetPosition(Vector3 position) => _position.Value = position;

        /// <summary> BossEnemyの回転を設定する </summary>
        /// <param name="rotation"> 新しい回転 </param>
        public void SetRotation(Quaternion rotation) => _rotation.Value = rotation;

        /// <summary> BossEnemy </summary>
        /// <param name="damage"></param>
        public void TakeDamage(int damage)
        {
            if(_currentHP.Value - damage <= 0)
            {
                _currentHP.Value = 0;
                return;
            }

            _currentHP.Value -= damage;
        }

        [SerializeField, Tooltip("このボスのEntryNode")]
        private TreeNode _originNode = null;

        [Header("ボスの最大HP")]
        [SerializeField, Tooltip("BossEnemyの最大HP")]
        private int _maxHP = 10000;

        [Header("ボスの攻撃力")]
        [SerializeField, Tooltip("BossEnemyの攻撃力")]
        private int _attackPower = 100;

        [Header("BossEnemyの各部位の防御力")]
        [SerializeField, Tooltip("頭の防御力")] private int _headDefense = 10;
        [SerializeField, Tooltip("胴体の防御力")] private int _bodyDefense = 100;
        [SerializeField, Tooltip("腕の防御力")] private int _armDefense = 100;
        [SerializeField, Tooltip("足の防御力")] private int _legDefense = 100;

        [Header("ボスが装着する各ArmerのData")]
        [SerializeField, Tooltip("右手Armer")] private BossArmerData _rightArmArmer;
        [SerializeField, Tooltip("左手Armer")] private BossArmerData _leftArmArmer;
        [SerializeField, Tooltip("右足Armer")] private BossArmerData _rightLegArmer;
        [SerializeField, Tooltip("左足Armer")] private BossArmerData _leftLegArmer;

        // BossEnemyの座標
        private ReactiveProperty<Vector3> _position;

        // BossEnemyの回転座標
        private ReactiveProperty<Quaternion> _rotation;

        // BossEnemyの現在のHP
        private ReactiveProperty<int> _currentHP;

        // BossEnemyの各部位の現在の防御力
        private int _currentHeadDefense;
        private int _currentBodyDefense;
        private int _currentaArmDefense;
        private int _currentLegDefense;
    }

    #region BossArmerData
    [Serializable]
    /// <summary> BossEnemyが装着するArmerのData </summary>
    public class BossArmerData
    {
        /// <summary> HPの最大値 </summary>
        public int MaxHP => _maxHP;

        /// <summary> 現在のHP </summary>
        public IReadOnlyReactiveProperty<int> CurrentHP => _currentHP;

        /// <summary> 防御力 </summary>
        public IReadOnlyReactiveProperty<int> Defense => _currentDefense;

        /// <summary> アーマー破壊フラグ </summary>
        public bool IsArmerBreak => _isArmerBreak;

        /// <summary> Armerの初期化メソッド </summary>
        public void Init()
        {
            Repair();
            _currentDefense.Value = _defense;
        }

        /// <summary> Armerの修復メソッド </summary>
        public void Repair()
        {
            _currentHP.Value = _maxHP;
            _isArmerBreak = false;
        }

        /// <summary> Armerへのダメージメソッド </summary>
        /// <param name="damage"> ダメージ総量 </param>
        public void Damage(int damage)
        {
            _currentHP.Value -= damage;

            if (_currentHP.Value < 0)
            {
                _currentHP.Value = 0;
                _isArmerBreak = true;
            }
        }

        [Header("最大HP")]
        [SerializeField, Tooltip("BossArmerの最大HP")]
        private int _maxHP = 1000;

        [Header("防御力")]
        [SerializeField, Tooltip("防御力")]
        private int _defense = 100;

        // 現在のHP
        private ReactiveProperty<int> _currentHP;

        // 現在の防御力
        private ReactiveProperty<int> _currentDefense;

        // アーマーのHPが0になって壊れた際にTrueになるフラグ
        private bool _isArmerBreak = false;
    }
    #endregion
}
