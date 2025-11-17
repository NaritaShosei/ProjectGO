using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unit tests for PlayerAttacker focusing on death handling
/// </summary>
public class PlayerAttackerTests
{
    private GameObject _playerObject;
    private PlayerAttacker _playerAttacker;

    [SetUp]
    public void SetUp()
    {
        _playerObject = new GameObject("TestPlayer");
        _playerAttacker = _playerObject.AddComponent<PlayerAttacker>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_playerObject != null)
        {
            Object.DestroyImmediate(_playerObject);
        }
    }

    [Test]
    public void Dead_Method_Exists()
    {
        var deadMethod = typeof(PlayerAttacker).GetMethod("Dead", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(deadMethod, "Dead method should exist");
    }

    [Test]
    public void CancelAttack_Method_Exists()
    {
        var cancelMethod = typeof(PlayerAttacker).GetMethod("CancelAttack", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(cancelMethod, "CancelAttack method should exist");
    }
}