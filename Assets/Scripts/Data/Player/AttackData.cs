using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "GameData/AttackData")]
public class AttackData : ScriptableObject
{
    public int AttackId => _attackId;
    public PlayerMode Mode => _mode;

    public int NextComboAttackId => _nextComboAttackId;
    public int InsertAfterAttackId => _insertAfterAttackId;

    public bool IsUnlockedBySkill => _isUnlockedBySkill;
    public int RequiredSkillId => _requiredSkillId;

    public IReadOnlyList<AttackVariantData> Variants => _variants;

    public AttackVariantData GetVariant(ChargeLevel chargeLevel)
    {
        foreach (var variant in _variants)
        {
            if (variant.RequiredCharge == chargeLevel)
            {
                return variant;
            }
        }
        return null; // 該当するバリアントがない場合
    }

    public void AddVariant(AttackVariantData variant)
    {
        _variants.Add(variant);
    }

    [Header("基本情報")]
    [SerializeField] private int _attackId; // 攻撃ID
    [SerializeField] private PlayerMode _mode; // 闘神 or 雷神

    [Header("Combo")]
    [Tooltip("次のコンボ攻撃ID。-1の場合はコンボ終了。")]
    [SerializeField] private int _nextComboAttackId = -1;
    [Tooltip("この差し込み攻撃を発動する起点となるAttackDataのID。-1で無効。")]
    [SerializeField] private int _insertAfterAttackId = -1;

    [Header("Skill Unlock")]
    [Tooltip("スキル解放が必要な攻撃かどうか")]
    [SerializeField] private bool _isUnlockedBySkill = false;
    [Tooltip("解放に必要なスキルID")]
    [SerializeField] private int _requiredSkillId = -1;

    [Header("攻撃バリアント")]
    [SerializeField] private List<AttackVariantData> _variants = new();
}

// 攻撃の段階（チャージレベル）
public enum ChargeLevel
{
    [InspectorName("溜めなし")]
    None = 0,
    [InspectorName("溜め1")]
    Level1 = 1,
    [InspectorName("溜め2")]
    Level2 = 2,
    [InspectorName("溜め3")]
    Level3 = 3
}

// 攻撃タイプ
public enum AttackType
{
    [InspectorName("弱攻撃")]
    LightAttack,
}

// モード
public enum PlayerMode
{
    [InspectorName("闘神")]
    Warrior,
    [InspectorName("雷神")]
    Thunder
}

#if UNITY_EDITOR

[CustomEditor(typeof(AttackData))]
public class AttackDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AttackData data = (AttackData)target;

        if (GUILayout.Button("攻撃バリアント追加"))
        {
            Undo.RecordObject(data, "Add Variant");

            var variant = new AttackVariantData();
            variant.SetDefaults();

            data.AddVariant(variant);

            EditorUtility.SetDirty(data);
        }
    }
}

#endif
