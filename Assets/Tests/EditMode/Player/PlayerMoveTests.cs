using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unit tests for PlayerMove focusing on CharacterData integration
/// </summary>
public class PlayerMoveTests
{
    private GameObject _playerObject;
    private PlayerMove _playerMove;

    [SetUp]
    public void SetUp()
    {
        _playerObject = new GameObject("TestPlayer");
        _playerMove = _playerObject.AddComponent<PlayerMove>();
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
    public void Init_AcceptsCharacterData()
    {
        var initMethod = typeof(PlayerMove).GetMethod("Init", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var parameters = initMethod?.GetParameters();
        
        Assert.IsNotNull(initMethod, "Init method should exist");
        Assert.AreEqual(4, parameters?.Length, "Init should accept 4 parameters");
        Assert.AreEqual(typeof(CharacterData), parameters?[3].ParameterType, "Fourth parameter should be CharacterData");
    }
}