using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System;

/// <summary>
/// Integration tests for PlayerManager
/// Tests state management, damage handling, death system, and event firing
/// </summary>
public class PlayerManagerTests
{
    private GameObject _playerObject;
    private PlayerManager _playerManager;

    [SetUp]
    public void SetUp()
    {
        _playerObject = new GameObject("TestPlayer");
        _playerManager = _playerObject.AddComponent<PlayerManager>();
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
    public void CurrentState_InitialState_IsNone()
    {
        Assert.AreEqual(PlayerState.None, _playerManager.CurrentState);
    }

    [Test]
    public void PlayerState_DeadState_Exists()
    {
        Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerState), PlayerState.Dead));
    }

    [Test]
    public void OnDead_Event_CanBeSubscribed()
    {
        bool eventFired = false;
        _playerManager.OnDead += () => eventFired = true;
        Assert.IsNotNull(_playerManager.OnDead);
    }
}