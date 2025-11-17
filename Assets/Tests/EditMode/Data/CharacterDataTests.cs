using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unit tests for CharacterData ScriptableObject
/// Tests property accessibility and data integrity
/// </summary>
public class CharacterDataTests
{
    private CharacterData _characterData;

    [SetUp]
    public void SetUp()
    {
        _characterData = ScriptableObject.CreateInstance<CharacterData>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_characterData != null)
        {
            Object.DestroyImmediate(_characterData);
        }
    }

    #region Property Tests

    [Test]
    public void Name_Property_CanBeAccessed()
    {
        var nameField = typeof(CharacterData).GetField("_name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        nameField?.SetValue(_characterData, "TestCharacter");
        
        Assert.AreEqual("TestCharacter", _characterData.Name, "Name property should return the set value");
    }

    [Test]
    public void MaxHP_Property_CanBeAccessed()
    {
        var maxHPField = typeof(CharacterData).GetField("_maxHP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        maxHPField?.SetValue(_characterData, 150f);
        
        Assert.AreEqual(150f, _characterData.MaxHP, "MaxHP property should return the set value");
    }

    [Test]
    public void MaxStamina_Property_CanBeAccessed()
    {
        var maxStaminaField = typeof(CharacterData).GetField("_maxStamina", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        maxStaminaField?.SetValue(_characterData, 75f);
        
        Assert.AreEqual(75f, _characterData.MaxStamina, "MaxStamina property should return the set value");
    }

    [Test]
    public void MoveSpeed_Property_CanBeAccessed()
    {
        var moveSpeedField = typeof(CharacterData).GetField("_moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        moveSpeedField?.SetValue(_characterData, 7.5f);
        
        Assert.AreEqual(7.5f, _characterData.MoveSpeed, "MoveSpeed property should return the set value");
    }

    [Test]
    public void Properties_AreReadOnly()
    {
        var nameProperty = typeof(CharacterData).GetProperty("Name");
        var maxHPProperty = typeof(CharacterData).GetProperty("MaxHP");
        var maxStaminaProperty = typeof(CharacterData).GetProperty("MaxStamina");
        var moveSpeedProperty = typeof(CharacterData).GetProperty("MoveSpeed");
        
        Assert.IsNull(nameProperty.GetSetMethod(), "Name should not have a public setter");
        Assert.IsNull(maxHPProperty.GetSetMethod(), "MaxHP should not have a public setter");
        Assert.IsNull(maxStaminaProperty.GetSetMethod(), "MaxStamina should not have a public setter");
        Assert.IsNull(moveSpeedProperty.GetSetMethod(), "MoveSpeed should not have a public setter");
    }

    #endregion
}