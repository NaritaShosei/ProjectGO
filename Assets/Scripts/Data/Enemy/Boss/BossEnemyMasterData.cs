using BossEnemy.BehaviorTree;
using System;
using UniRx;
using UnityEditor;
using UnityEngine;

// BossEnemyに関するData
namespace BossEnemy.Data
{
    /// <summary> ボスエネミーを構成する各パーツの種類 </summary>
    public enum BodysDefensesType
    {
        None, // default値
        Hard, // 硬い
        Normal, // そこそこ
        WeekPoint, // 弱点
        VitalPoint // 急所
    }

    /// <summary> 1体のBossEnemyのマスターデータをPhaseごとに分けて保持するホルダークラス </summary>

    public class BossEnemyMasterData 
    {
        public BossEnemyData[] BossEnemyDatas => _bossEnemyDatas;
        public string BossName => _bossName;
        public int TotalPhaseCount => _totalPhaseCount;

        public BossEnemyData GetData(int index)
        {
            if (_bossEnemyDatas.Length <= index) Debug.LogError("indexが存在しません");

            return _bossEnemyDatas[index];
        }

        [Header("各フェーズのBossEnemyのマスターデータ")]
        [SerializeField, Tooltip("BossEnemyのマスターデータ")]
        private BossEnemyData[] _bossEnemyDatas;

        // 名前
        [SerializeField, Tooltip("BossEnemy name")]
        private string _bossName;

        // 全てのPhase数
        private int _totalPhaseCount;

        #region DataConstruct
#if UNITY_EDITOR
        /// <summary>
        /// Dataの構築メソッド
        /// </summary>
        /// <param name="bossEnemyDatas"> 各Phaseのボスエネミーデータ </param>
        /// <param name="bossName"> 名前 </param>
        public void DataConstruct(BossEnemyData[] bossEnemyDatas, string bossName)
        {
            _bossEnemyDatas = bossEnemyDatas;
            _bossName = bossName;
            _totalPhaseCount = _bossEnemyDatas.Length;
        }
#endif
        #endregion
    }

    #region ボスエネミー本体のデータ
    [Serializable]
    public class BossEnemyData
    {
        /// <summary> 現在のHP </summary>
        public IReadOnlyReactiveProperty<int> CurrentHP => _currentHP;

        /// <summary> BossEnemyの座標データ </summary>
        public IReadOnlyReactiveProperty<Vector3> Position => _position;

        /// <summary> BossEnemyの回転データ </summary>
        public IReadOnlyReactiveProperty<Quaternion> Rotation => _rotation;

        /// <summary> BossEnemyの移動速度 </summary>
        public IReadOnlyReactiveProperty<Vector3> Velocity => _velocity;

        /// <summary> BossEnemyの衝突判定 </summary>
        public IReadOnlyReactiveProperty<bool> IsTigger => _isTrigger;

        /// <summary> 最大HP </summary>
        public int MaxHP => _maxHP;

        /// <summary> 歩行速度 </summary>
        public float WalkSpeed => _walkSpeed;

        /// <summary> 硬い箇所の防御力 </summary>
        public int HardSpotsDefense => _currentHardSpotsDefense;

        /// <summary> そこそこ硬い箇所の防御力 </summary>
        public int NormalSpotsDefense => _currentNormalSpotsDefense;

        /// <summary> 弱点の防御力 </summary>
        public int WeekPointDefense => _currentaWeekPointDefense;

        /// <summary> 急所の防御力 </summary>
        public int VitalPointDefense => _currentVitalPointDefense;

        /// <summary> ボスが装着する右手ArmerのData </summary>
        public BossArmorData RightArmArmer => _rightArmArmer;

        /// <summary> ボスが装着する左手ArmerのData </summary>
        public BossArmorData LeftArmArmer => _leftArmArmer;

        /// <summary> ボスが装着する右足ArmerのData</summary>
        public BossArmorData RightLegArmer => _rightLegArmer;

        /// <summary> ボスが装着する左足ArmerのData </summary>
        public BossArmorData LeftLegArmer => _leftLegArmer;

        /// <summary> 通常近距離攻撃 </summary>
        public BossEnemyAttackField CloseRangeNormalAttackFieldHolder => _closeRangeNormalAttackDataHolder;

        /// <summary> 通常近距離攻撃カウント3到達時特殊近距離攻撃 </summary>
        public BossEnemyAttackField CloseRangeFinishCountAttackFieldHolder => _closeRangeFinishCountAttackDataHolder;

        /// <summary> 遠距離攻撃 </summary>
        public BossEnemyAttackField LongRangeAttackFieldHolder => _longRangeAttackDataHolder;

        /// <summary> BossEnemyの初期化メソッド </summary>
        public void Init(Transform bossEnemyTransform)
        {
            // 現在の座標をセット
            _position = new(bossEnemyTransform.position);

            // 現在の回転座標をセット
            _rotation = new(bossEnemyTransform.rotation);

            // BossEnemyの移動速度を初期化
            _velocity = new(Vector3.zero);

            // HPを最大値にする
            _currentHP = new(_maxHP);

            // 衝突判定の初期化
            _isTrigger = new(false);

            // 各アーマーの初期化
            _rightArmArmer.Init();
            _leftArmArmer.Init();
            _rightLegArmer.Init();
            _leftLegArmer.Init();

            // 各パーツの防御力を初期化
            _currentHardSpotsDefense = _hardSpotsDefense;
            _currentNormalSpotsDefense = _normalSpotsDefense;
            _currentaWeekPointDefense = _weekPointDefense;
            _currentVitalPointDefense = _vitalPointDefense;
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

        /// <summary> BossEnemyの体の衝突判定を設定する </summary>
        /// <param name="velocity"> 衝突判定 </param>
        public void SetColliderIsTrigger(bool isCollision) => _isTrigger.Value = isCollision;

        /// <summary> BossEnemyダメージ </summary>
        public void TakeDamage(int damage)
        {
            if (_currentHP.Value - damage <= 0)
            {
                _currentHP.Value = 0;
                return;
            }

            _currentHP.Value -= damage;

            Debug.Log("現在のHP：" +  _currentHP.Value);
        }

        [Header("ボスの最大HP")]
        [SerializeField, Tooltip("BossEnemyの最大HP")]
        private int _maxHP = 10000;

        [Header("ボスの歩行速度")]
        [SerializeField, Tooltip("ボスの歩行速度")]
        private float _walkSpeed = 10000;

        [Header("BossEnemyの硬度(肉質)の値")]
        [SerializeField, Tooltip("硬度(肉質)の高い部位の硬さ")] private int _hardSpotsDefense = 30;
        [SerializeField, Tooltip("硬度(肉質)のそこそこ高い部位の硬さ")] private int _normalSpotsDefense = 100;
        [SerializeField, Tooltip("弱点の部位の硬さ")] private int _weekPointDefense = 120;
        [SerializeField, Tooltip("急所の部位の硬さ")] private int _vitalPointDefense = 150;

        [Header("ボスが装着する各ArmorのData")]
        [SerializeField, Tooltip("右手Armor")] private BossArmorData _rightArmArmer;
        [SerializeField, Tooltip("左手Armor")] private BossArmorData _leftArmArmer;
        [SerializeField, Tooltip("右足Armor")] private BossArmorData _rightLegArmer;
        [SerializeField, Tooltip("左足Armor")] private BossArmorData _leftLegArmer;

        [Header("特定条件下での発動可能攻撃データ")]

        [Header("通常近距離攻撃")]
        [SerializeField, Tooltip("通常近距離攻撃")] 
        private BossEnemyAttackField _closeRangeNormalAttackDataHolder;

        [Header("通常近距離攻撃3カウント到達時の近距離攻撃")]
        [SerializeField, Tooltip("通常近距離攻撃3回到達時の特殊近距離攻撃")]
        private BossEnemyAttackField _closeRangeFinishCountAttackDataHolder;

        [Header("遠距離攻撃")]
        [SerializeField, Tooltip("遠距離攻撃")]
        private BossEnemyAttackField _longRangeAttackDataHolder;

        // BossEnemyの座標
        private ReactiveProperty<Vector3> _position;

        // BossEnemyの回転座標
        private ReactiveProperty<Quaternion> _rotation;

        // BossEnemyの移動速度
        private ReactiveProperty<Vector3> _velocity;

        // BossEnemyの現在のHP
        private ReactiveProperty<int> _currentHP;

        // 衝突判定のオンオフフラグ
        private ReactiveProperty<bool> _isTrigger;

        // BossEnemyの各部位の現在の防御力
        private int _currentHardSpotsDefense;
        private int _currentNormalSpotsDefense;
        private int _currentaWeekPointDefense;
        private int _currentVitalPointDefense;

        #region DataConstruct
#if UNITY_EDITOR
        public void DataConstruct(int maxHP, float walkSpeed,
            int hardSpotsDefense, int normalSpotsDefense, int weekPointDefense, int vitalPointDefense,
            BossArmorData rightArmArmer, BossArmorData leftArmArmer, BossArmorData rightLegArmer, BossArmorData leftLegArmer,
            BossEnemyAttackField closeRangeNormalAttackDataHolder, 
            BossEnemyAttackField closeRangeFinishCountAttackDataHolder,
            BossEnemyAttackField longRangeAttackDataHolder)
        {
            _maxHP = maxHP;
            _walkSpeed = walkSpeed;
            _hardSpotsDefense = hardSpotsDefense;
            _normalSpotsDefense = normalSpotsDefense;
            _weekPointDefense = weekPointDefense;
            _vitalPointDefense = vitalPointDefense;
            _rightArmArmer = rightArmArmer;
            _leftArmArmer = leftArmArmer;
            _rightLegArmer = rightLegArmer;
            _leftLegArmer = leftLegArmer;
            _closeRangeNormalAttackDataHolder = closeRangeNormalAttackDataHolder;
            _closeRangeFinishCountAttackDataHolder = closeRangeFinishCountAttackDataHolder;
            _longRangeAttackDataHolder = longRangeAttackDataHolder;
        }
#endif
        #endregion
    }
    #endregion

    #region ボスエネミーがまとっている鎧のデータ
    [Serializable]
    /// <summary> BossEnemyが装着するArmerのData </summary>
    public class BossArmorData
    {
        /// <summary> HPの最大値 </summary>
        public int MaxHP => _maxHP;

        /// <summary> 現在のHP </summary>
        public IReadOnlyReactiveProperty<int> CurrentHP => _currentHP;

        /// <summary> 防御力 </summary>
        public int Defense => _currentDefense;

        /// <summary> アーマー破壊フラグ </summary>
        public bool IsArmorBreak => _isArmorBreak;

        /// <summary> Armerの初期化メソッド </summary>
        public void Init()
        {
            Debug.Log("アーマー初期化");
            if (_currentHP == null) _currentHP = new(_maxHP);
            else _currentHP.Value = _maxHP;
            _isArmorBreak = false;
            _currentDefense = _defense;
        }

        /// <summary> Armerの修復メソッド </summary>
        public void Repair()
        {
            _currentHP.Value = _maxHP;
            _isArmorBreak = false;
        }

        /// <summary> Armerへのダメージメソッド </summary>
        /// <param name="damage"> ダメージ総量 </param>
        public void Damage(int damage)
        {
            if (_currentHP.Value - damage <= 0)
            {
                BreakArmor();
                _currentHP.Value = 0;
                return;
            }

            _currentHP.Value -= damage;
        }

        [Header("最大HP")]
        [SerializeField, Tooltip("BossArmerの最大HP")]
        private int _maxHP = 1000;

        [Header("硬度(肉質)")]
        [SerializeField, Tooltip("硬度(肉質)")]
        private int _defense = 100;

        // 現在のHP
        private ReactiveProperty<int> _currentHP;

        // 現在の防御力
        private int _currentDefense;

        // アーマーのHPが0になって壊れた際にTrueになるフラグ
        private bool _isArmorBreak = false;

        private void BreakArmor()
        {
            _isArmorBreak = true;
        }

        #region DataConstruct
#if UNITY_EDITOR
        public void DataConstruct(int maxHP, int defense)
        {
            _maxHP = maxHP;
            _defense = defense;
        }
#endif
        #endregion
    }
    #endregion

    #region ボスエネミーの攻撃データ

    /// <summary> ボスエネミーの攻撃情報 </summary>
    [Serializable]
    public class BossEnemyAttackField
    {
        [Serializable]
        public struct AttackCondition
        {
            [Header("攻撃DataのID")]
            public int ID;

            [Header("攻撃の発動確率")]
            public int ActivationRate;
        }

        public AttackCondition[] AttackField => _attackField;

        [SerializeField, Tooltip("ボスエネミーの攻撃マスターデータ")]
        private AttackCondition[] _attackField;

        #region DataConstruct
#if UNITY_EDITOR
        public void DataConstruct(AttackCondition[] attackField)
        {
            _attackField = attackField;
        }
#endif
        #endregion
    }

    /// <summary> BossEnemyの攻撃データ </summary>
    public struct BossEnemyAttackData
    {
        public BossEnemyAttackData(
            int id, string name, float attackChargeTime, float attackDuration, float recoveryTime, float attackAreaEffectStartTime,
            float damage, float attackRange, float attackHitAreaCenterDistance, float attackStartDistance, float nockBackPower, float coolTime, string animParam)
        {
            _attackID = id;
            _attackName = name;
            _attackChargeTime = attackChargeTime;
            _attackDuration = attackDuration;
            _recoveryTime = recoveryTime;
            _attackAreaEffectStartTime = attackAreaEffectStartTime;
            _damage = damage;
            _attackRange = attackRange;
            _attackHitAreaCenterDistance = attackHitAreaCenterDistance;
            _attackStartDistance = attackStartDistance;
            _nockBackPower = nockBackPower;
            _coolTime = coolTime;
            _animParamName = animParam;
        }

        /// <summary> DataID </summary>
        public int ID => _attackID;
        /// <summary> 攻撃名称 </summary>
        public string Name => _attackName;
        /// <summary> 攻撃判定開始までの時間 </summary>
        public float AttackChargeTime => _attackChargeTime;
        /// <summary> 攻撃持続時間 </summary>
        public float AttackDuration => _attackDuration;
        /// <summary> 攻撃後硬直時間 </summary>
        public float RecoveryTime => _recoveryTime;
        /// <summary> 攻撃範囲エフェクト発生までの時間 </summary>
        public float AttackAreaEffectStartTime => _attackAreaEffectStartTime;
        /// <summary> 一撃のダメージ量 </summary>
        public float Damage => _damage;
        /// <summary> 攻撃範囲 </summary>
        public float AttackRange => _attackRange;
        /// <summary> 攻撃範囲の中心座標までの距離 </summary>
        public float AttackHitAreaCenterDistance => _attackHitAreaCenterDistance;
        /// <summary> 攻撃を開始する距離(どこからでも届く場合は0) </summary>
        public float AttackStartDistance => _attackStartDistance;
        /// <summary> ノックバックする力 </summary>
        public float NockBackPower => _nockBackPower;
        /// <summary> 攻撃のクールタイム </summary>
        public float CoolTime => _coolTime;
        /// <summary> アニメーションのパラメータ名 </summary>
        public string AnimParamName => _animParamName;

        // ID
        private int _attackID;

        // 攻撃の名称
        private string _attackName;

        // 発生時間
        private float _attackChargeTime;

        // 攻撃の持続時間
        private float _attackDuration;

        // 攻撃後硬直時間
        private float _recoveryTime;

        // 攻撃範囲エフェクト発生までの時間
        private float _attackAreaEffectStartTime;

        // 一撃のダメージ量
        private float _damage;
        
        // 攻撃範囲
        private float _attackRange;

        // 攻撃範囲の中心座標
        private float _attackHitAreaCenterDistance;

        // 攻撃が届く距離
        private float _attackStartDistance;

        // ノックバックする力
        private float _nockBackPower;

        // スキルのクールタイム
        private float _coolTime;

        // アニメーションのパラメータ名
        private string _animParamName;
    }
    #endregion

    #region ボスエネミーのマスターデータ編集用Editor拡張
#if UNITY_EDITOR
    [CustomEditor(typeof(BossEnemyMasterData))]
    public class BossEnemyDataHolderEditor : Editor
    {
        // ========== Style constants ==========
        private static readonly Color[] PhaseHeaderColors =
        {
        new Color(0.20f, 0.50f, 0.85f, 1f), // Phase 1 - Blue
        new Color(0.85f, 0.50f, 0.15f, 1f), // Phase 2 - Orange
        new Color(0.75f, 0.20f, 0.20f, 1f), // Phase 3 - Red
        new Color(0.50f, 0.20f, 0.75f, 1f), // Phase 4 - Purple
        new Color(0.20f, 0.65f, 0.30f, 1f), // Phase 5 - Green
    };

        private static readonly string[] PhaseIcons =
        {
        "◆", "◆", "◆", "◆", "◆"
    };

        private SerializedProperty _bossEnemyDatasProperty;
        private SerializedProperty _bossNameProperty;
        private bool[] _phasesFoldout;
        private bool _isInitialized = false;

        // 削除・移動はIMGUIループ外で実行するため描画後に処理
        private enum PendingActionType { None, Delete, MoveUp, MoveDown }
        private PendingActionType _pendingAction = PendingActionType.None;
        private int _pendingActionIndex = -1;

        // ========== GUIStyle cache ==========
        private GUIStyle _phaseHeaderStyle;
        private GUIStyle _addButtonStyle;
        private GUIStyle _removeButtonStyle;
        private GUIStyle _summaryLabelStyle;
        private bool _stylesInitialized = false;

        // ========== Lifecycle ==========
        private void OnEnable()
        {
            _bossEnemyDatasProperty = serializedObject.FindProperty("_bossEnemyDatas");
            _bossNameProperty = serializedObject.FindProperty("_bossName");
            InitializeFoldouts();
        }

        private void InitializeFoldouts()
        {
            int count = _bossEnemyDatasProperty.arraySize;
            _phasesFoldout = new bool[Mathf.Max(count, 8)];
            for (int i = 0; i < _phasesFoldout.Length; i++)
                _phasesFoldout[i] = true;
            _isInitialized = true;
        }

        private void EnsureFoldoutCapacity(int requiredSize)
        {
            if (_phasesFoldout == null || _phasesFoldout.Length < requiredSize)
            {
                bool[] newArr = new bool[requiredSize + 4];
                if (_phasesFoldout != null)
                    System.Array.Copy(_phasesFoldout, newArr, _phasesFoldout.Length);
                for (int i = _phasesFoldout?.Length ?? 0; i < newArr.Length; i++)
                    newArr[i] = true;
                _phasesFoldout = newArr;
            }
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _phaseHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            _addButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            _removeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                normal = { textColor = new Color(1f, 0.4f, 0.4f) },
                hover = { textColor = new Color(1f, 0.2f, 0.2f) }
            };

            _summaryLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) },
                alignment = TextAnchor.MiddleRight
            };

            _stylesInitialized = true;
        }

        // ========== Main Inspector GUI ==========
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            InitializeStyles();

            if (!_isInitialized) InitializeFoldouts();

            DrawHeader();
            EditorGUILayout.Space(4);
            DrawPhaseList();
            EditorGUILayout.Space(6);
            DrawFooterButtons();

            // 描画ループ完了後に配列操作を実行（途中変更によるDisposedException防止）
            ApplyPendingAction();

            serializedObject.ApplyModifiedProperties();
        }

        private void ApplyPendingAction()
        {
            if (_pendingAction == PendingActionType.None) return;

            int idx = _pendingActionIndex;
            switch (_pendingAction)
            {
                case PendingActionType.Delete:
                    _bossEnemyDatasProperty.DeleteArrayElementAtIndex(idx);
                    break;
                case PendingActionType.MoveUp:
                    _bossEnemyDatasProperty.MoveArrayElement(idx, idx - 1);
                    SwapFoldout(idx, idx - 1);
                    break;
                case PendingActionType.MoveDown:
                    _bossEnemyDatasProperty.MoveArrayElement(idx, idx + 1);
                    SwapFoldout(idx, idx + 1);
                    break;
            }

            _pendingAction = PendingActionType.None;
            _pendingActionIndex = -1;
            // 変更を即反映してInspectorを再描画
            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        // ========== Header ==========
        private void DrawHeader()
        {
            using var headerScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Boss Enemy Phase Data", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                normal = { textColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.85f, 0.3f)
                : new Color(0.55f, 0.35f, 0f) }
            });

            EditorGUILayout.PropertyField(_bossNameProperty, new GUIContent("Boss Name"));
            EditorGUILayout.Space(2);

            int count = _bossEnemyDatasProperty.arraySize;
            string summary = count == 0
                ? "No phases configured"
                : $"{count} phase{(count > 1 ? "s" : "")} configured";

            EditorGUILayout.LabelField(summary, EditorStyles.miniLabel);
        }

        // ========== Phase List ==========
        private void DrawPhaseList()
        {
            int count = _bossEnemyDatasProperty.arraySize;
            EnsureFoldoutCapacity(count);

            if (count == 0)
            {
                DrawEmptyState();
                return;
            }

            for (int i = 0; i < count; i++)
            {
                DrawPhaseBlock(i, count);
                EditorGUILayout.Space(2);
            }
        }

        private void DrawEmptyState()
        {
            EditorGUILayout.Space(4);
            using var scope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            GUILayout.Label(
                "↓ 下の「Phase を追加」ボタンでPhaseを追加してください",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true }
            );
            EditorGUILayout.Space(4);
        }

        // ========== Single Phase Block ==========
        private void DrawPhaseBlock(int index, int totalCount)
        {
            // このフレームで削除予定の要素は描画しない
            if (_pendingAction == PendingActionType.Delete && _pendingActionIndex == index)
                return;

            SerializedProperty phaseData = _bossEnemyDatasProperty.GetArrayElementAtIndex(index);

            Color headerColor = GetPhaseColor(index);
            Color bgColor = new Color(
                headerColor.r * 0.25f,
                headerColor.g * 0.25f,
                headerColor.b * 0.25f,
                0.25f
            );

            // Outer container
            Rect blockRect = EditorGUILayout.BeginVertical();
            DrawColoredBackground(blockRect, bgColor, 4f);

            // ---- Phase header bar ----
            DrawPhaseHeader(index, totalCount, phaseData, headerColor);

            // ---- Folded content ----
            if (_phasesFoldout[index])
            {
                EditorGUILayout.Space(2);
                DrawPhaseContent(phaseData, index);
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndVertical();

            // Draw border
            if (Event.current.type == EventType.Repaint)
            {
                DrawBorder(blockRect, new Color(headerColor.r, headerColor.g, headerColor.b, 0.5f), 4f);
            }
        }

        private void DrawPhaseHeader(int index, int totalCount, SerializedProperty phaseData, Color headerColor)
        {
            Rect headerRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(28));

            // Color bar on left
            if (Event.current.type == EventType.Repaint)
            {
                Rect barRect = new Rect(headerRect.x, headerRect.y, 4f, headerRect.height);
                EditorGUI.DrawRect(barRect, headerColor);
            }

            GUILayout.Space(10);

            // Foldout arrow + Phase label
            string icon = _phasesFoldout[index] ? "▼" : "▶";
            string phaseName = GetPhaseDisplayName(phaseData, index);
            string label = $"{icon}  Phase {index + 1}  —  {phaseName}";

            if (GUILayout.Button(label, _phaseHeaderStyle, GUILayout.ExpandWidth(true), GUILayout.Height(24)))
            {
                _phasesFoldout[index] = !_phasesFoldout[index];
            }
            // Reset button style background
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            // Move Up / Down buttons
            EditorGUI.BeginDisabledGroup(index == 0);
            if (GUILayout.Button("↑", GUILayout.Width(24), GUILayout.Height(22)))
            {
                _pendingAction = PendingActionType.MoveUp;
                _pendingActionIndex = index;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(index == totalCount - 1);
            if (GUILayout.Button("↓", GUILayout.Width(24), GUILayout.Height(22)))
            {
                _pendingAction = PendingActionType.MoveDown;
                _pendingActionIndex = index;
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(4);

            // Remove button
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f, 0.85f);
            if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog(
                    "Phase 削除",
                    $"Phase {index + 1} を削除しますか？\nこの操作は元に戻せません。",
                    "削除する", "キャンセル"))
                {
                    _pendingAction = PendingActionType.Delete;
                    _pendingActionIndex = index;
                }
            }
            GUI.backgroundColor = prevBg;

            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPhaseContent(SerializedProperty phaseData, int phaseIndex)
        {
            EditorGUI.indentLevel++;
            using var contentScope = new EditorGUILayout.VerticalScope();

            // Draw all child properties of BossEnemyData
            SerializedProperty iterator = phaseData.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();

            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;
                EditorGUILayout.PropertyField(iterator, true);
            }

            EditorGUI.indentLevel--;
        }

        // ========== Footer Buttons ==========
        private void DrawFooterButtons()
        {
            using var scope = new EditorGUILayout.HorizontalScope();

            // Add Phase
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.35f, 0.75f, 0.35f, 1f);
            if (GUILayout.Button("＋  Phase を追加", _addButtonStyle, GUILayout.Height(30)))
            {
                int newIndex = _bossEnemyDatasProperty.arraySize;
                _bossEnemyDatasProperty.arraySize++;
                EnsureFoldoutCapacity(newIndex + 1);
                _phasesFoldout[newIndex] = true;
            }
            GUI.backgroundColor = prev;

            GUILayout.Space(6);

            // Expand/Collapse All
            if (GUILayout.Button("全て開く", GUILayout.Height(30), GUILayout.Width(80)))
                SetAllFoldouts(true);

            if (GUILayout.Button("全て閉じる", GUILayout.Height(30), GUILayout.Width(80)))
                SetAllFoldouts(false);
        }

        // ========== Helpers ==========
        private string GetPhaseDisplayName(SerializedProperty phaseData, int index)
        {
            // Try to find a "name" or "_name" field for display
            SerializedProperty nameProp =
                phaseData.FindPropertyRelative("PhaseName") ??
                phaseData.FindPropertyRelative("_phaseName") ??
                phaseData.FindPropertyRelative("Name") ??
                phaseData.FindPropertyRelative("_name");

            if (nameProp != null && nameProp.propertyType == SerializedPropertyType.String
                && !string.IsNullOrWhiteSpace(nameProp.stringValue))
            {
                return nameProp.stringValue;
            }

            return "未設定";
        }

        private Color GetPhaseColor(int index)
        {
            if (PhaseHeaderColors != null && PhaseHeaderColors.Length > 0)
                return PhaseHeaderColors[index % PhaseHeaderColors.Length];
            return new Color(0.3f, 0.5f, 0.8f);
        }

        private void SetAllFoldouts(bool value)
        {
            if (_phasesFoldout == null) return;
            for (int i = 0; i < _phasesFoldout.Length; i++)
                _phasesFoldout[i] = value;
        }

        private void SwapFoldout(int a, int b)
        {
            EnsureFoldoutCapacity(Mathf.Max(a, b) + 1);
            bool tmp = _phasesFoldout[a];
            _phasesFoldout[a] = _phasesFoldout[b];
            _phasesFoldout[b] = tmp;
        }

        private void DrawColoredBackground(Rect rect, Color color, float radius)
        {
            if (Event.current.type != EventType.Repaint || rect.width <= 0) return;
            EditorGUI.DrawRect(rect, color);
        }

        private void DrawBorder(Rect rect, Color color, float radius)
        {
            float t = 1.5f;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, t), color);           // top
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - t, rect.width, t), color);    // bottom
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, t, rect.height), color);          // left
            EditorGUI.DrawRect(new Rect(rect.xMax - t, rect.y, t, rect.height), color);   // right
        }
    }
#endif
    #endregion
}
