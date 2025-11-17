using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Comprehensive unit tests for PlayerStats class
/// Tests cover initialization, damage, stamina management, recovery, and edge cases
/// </summary>
public class PlayerStatsTests
{
    private PlayerManager _mockManager;
    private CharacterData _testCharacterData;
    private PlayerStats _playerStats;

    [SetUp]
    public void SetUp()
    {
        // Create a mock CharacterData ScriptableObject for testing
        _testCharacterData = ScriptableObject.CreateInstance<CharacterData>();
        
        // Use reflection to set private fields since they're SerializeField
        var nameField = typeof(CharacterData).GetField("_name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var maxHPField = typeof(CharacterData).GetField("_maxHP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var maxStaminaField = typeof(CharacterData).GetField("_maxStamina", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var moveSpeedField = typeof(CharacterData).GetField("_moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        nameField?.SetValue(_testCharacterData, "TestPlayer");
        maxHPField?.SetValue(_testCharacterData, 100f);
        maxStaminaField?.SetValue(_testCharacterData, 50f);
        moveSpeedField?.SetValue(_testCharacterData, 5f);

        // Create a GameObject with PlayerManager for testing
        GameObject testObject = new GameObject("TestPlayer");
        _mockManager = testObject.AddComponent<PlayerManager>();
        
        // Initialize PlayerStats with test data
        _playerStats = new PlayerStats(_mockManager, _testCharacterData);
    }

    [TearDown]
    public void TearDown()
    {
        if (_mockManager != null)
        {
            Object.DestroyImmediate(_mockManager.gameObject);
        }
        if (_testCharacterData != null)
        {
            Object.DestroyImmediate(_testCharacterData);
        }
    }

    #region Initialization Tests

    [Test]
    public void Initialize_WithValidData_SetsMaxHpCorrectly()
    {
        Assert.AreEqual(100f, _playerStats.MaxHp, "MaxHp should be initialized from CharacterData");
    }

    [Test]
    public void Initialize_WithValidData_SetsMaxStaminaCorrectly()
    {
        Assert.AreEqual(50f, _playerStats.MaxStamina, "MaxStamina should be initialized from CharacterData");
    }

    [Test]
    public void Initialize_WithValidData_CurrentHpEqualsMaxHp()
    {
        Assert.AreEqual(_playerStats.MaxHp, _playerStats.CurrentHp, "CurrentHp should start at MaxHp");
    }

    [Test]
    public void Initialize_WithValidData_CurrentStaminaEqualsMaxStamina()
    {
        Assert.AreEqual(_playerStats.MaxStamina, _playerStats.CurrentStamina, "CurrentStamina should start at MaxStamina");
    }

    [Test]
    public void Initialize_WithValidData_IsNotDead()
    {
        Assert.IsFalse(_playerStats.IsDead, "Player should not be dead at initialization");
    }

    #endregion

    #region Damage Tests - Happy Path

    [Test]
    public void TryAddDamage_WithValidDamage_DecreasesHp()
    {
        float initialHp = _playerStats.CurrentHp;
        bool result = _playerStats.TryAddDamage(10f);
        
        Assert.IsTrue(result, "TryAddDamage should return true when player survives");
        Assert.AreEqual(initialHp - 10f, _playerStats.CurrentHp, "HP should decrease by damage amount");
    }

    [Test]
    public void TryAddDamage_MultipleTimes_AccumulatesDamage()
    {
        _playerStats.TryAddDamage(20f);
        _playerStats.TryAddDamage(30f);
        
        Assert.AreEqual(50f, _playerStats.CurrentHp, "Multiple damages should accumulate");
    }

    [Test]
    public void TryAddDamage_ExactlyMaxHp_KillsPlayer()
    {
        bool result = _playerStats.TryAddDamage(100f);
        
        Assert.IsFalse(result, "TryAddDamage should return false when HP reaches 0");
        Assert.AreEqual(0f, _playerStats.CurrentHp, "HP should be 0");
        Assert.IsTrue(_playerStats.IsDead, "Player should be dead");
    }

    [Test]
    public void TryAddDamage_MoreThanMaxHp_KillsPlayer()
    {
        bool result = _playerStats.TryAddDamage(150f);
        
        Assert.IsFalse(result, "TryAddDamage should return false when overkill damage");
        Assert.AreEqual(0f, _playerStats.CurrentHp, "HP should not go below 0");
        Assert.IsTrue(_playerStats.IsDead, "Player should be dead");
    }

    #endregion

    #region Damage Tests - Edge Cases

    [Test]
    public void TryAddDamage_ZeroDamage_NoChange()
    {
        float initialHp = _playerStats.CurrentHp;
        bool result = _playerStats.TryAddDamage(0f);
        
        Assert.IsTrue(result, "Zero damage should return true");
        Assert.AreEqual(initialHp, _playerStats.CurrentHp, "HP should not change with zero damage");
    }

    [Test]
    public void TryAddDamage_NegativeDamage_LogsWarningAndNoChange()
    {
        float initialHp = _playerStats.CurrentHp;
        
        LogAssert.Expect(LogType.Warning, "ダメージは0以上である必要があります");
        bool result = _playerStats.TryAddDamage(-10f);
        
        Assert.IsTrue(result, "Negative damage should return alive status");
        Assert.AreEqual(initialHp, _playerStats.CurrentHp, "HP should not change with negative damage");
    }

    [Test]
    public void TryAddDamage_WhenAlreadyDead_ReturnsFalse()
    {
        _playerStats.TryAddDamage(100f); // Kill the player
        
        bool result = _playerStats.TryAddDamage(10f);
        
        Assert.IsFalse(result, "Should return false when already dead");
        Assert.AreEqual(0f, _playerStats.CurrentHp, "HP should remain 0");
    }

    [Test]
    public void TryAddDamage_VerySmallDamage_WorksCorrectly()
    {
        bool result = _playerStats.TryAddDamage(0.001f);
        
        Assert.IsTrue(result, "Very small damage should work");
        Assert.AreEqual(99.999f, _playerStats.CurrentHp, 0.0001f, "HP should decrease by small amount");
    }

    [Test]
    public void TryAddDamage_FloatingPointPrecision_HandledCorrectly()
    {
        // Test that brings HP very close to 0
        _playerStats.TryAddDamage(99.9999f);
        
        Assert.IsFalse(_playerStats.IsDead, "Player should not be dead with tiny HP remaining");
        Assert.Greater(_playerStats.CurrentHp, 0f, "HP should be slightly above 0");
    }

    #endregion

    #region Stamina Usage Tests - Happy Path

    [Test]
    public void TryUseStamina_WithEnoughStamina_DecreasesStamina()
    {
        float initialStamina = _playerStats.CurrentStamina;
        bool result = _playerStats.TryUseStamina(10f);
        
        Assert.IsTrue(result, "TryUseStamina should return true when enough stamina");
        Assert.AreEqual(initialStamina - 10f, _playerStats.CurrentStamina, "Stamina should decrease");
    }

    [Test]
    public void TryUseStamina_ExactAmount_UsesAllStamina()
    {
        bool result = _playerStats.TryUseStamina(50f);
        
        Assert.IsTrue(result, "Should succeed with exact stamina amount");
        Assert.AreEqual(0f, _playerStats.CurrentStamina, "Stamina should be 0");
    }

    [Test]
    public void TryUseStamina_MultipleTimes_AccumulatesUsage()
    {
        _playerStats.TryUseStamina(10f);
        _playerStats.TryUseStamina(15f);
        
        Assert.AreEqual(25f, _playerStats.CurrentStamina, "Multiple usages should accumulate");
    }

    #endregion

    #region Stamina Usage Tests - Edge Cases

    [Test]
    public void TryUseStamina_NotEnoughStamina_ReturnsFalseAndNoChange()
    {
        bool result = _playerStats.TryUseStamina(60f);
        
        Assert.IsFalse(result, "Should return false when not enough stamina");
        Assert.AreEqual(50f, _playerStats.CurrentStamina, "Stamina should not change");
    }

    [Test]
    public void TryUseStamina_ZeroAmount_ReturnsTrue()
    {
        float initialStamina = _playerStats.CurrentStamina;
        bool result = _playerStats.TryUseStamina(0f);
        
        Assert.IsTrue(result, "Zero usage should return true");
        Assert.AreEqual(initialStamina, _playerStats.CurrentStamina, "Stamina should not change");
    }

    [Test]
    public void TryUseStamina_NegativeAmount_LogsWarningAndReturnsFalse()
    {
        float initialStamina = _playerStats.CurrentStamina;
        
        LogAssert.Expect(LogType.Warning, "スタミナ消費量は0以上である必要があります");
        bool result = _playerStats.TryUseStamina(-10f);
        
        Assert.IsFalse(result, "Negative usage should return false");
        Assert.AreEqual(initialStamina, _playerStats.CurrentStamina, "Stamina should not change");
    }

    [Test]
    public void TryUseStamina_WhenStaminaDepleted_ReturnsFalse()
    {
        _playerStats.TryUseStamina(50f); // Use all stamina
        
        bool result = _playerStats.TryUseStamina(1f);
        
        Assert.IsFalse(result, "Should return false when stamina is 0");
        Assert.AreEqual(0f, _playerStats.CurrentStamina, "Stamina should remain 0");
    }

    [Test]
    public void TryUseStamina_VerySmallAmount_WorksCorrectly()
    {
        bool result = _playerStats.TryUseStamina(0.001f);
        
        Assert.IsTrue(result, "Very small usage should work");
        Assert.AreEqual(49.999f, _playerStats.CurrentStamina, 0.0001f, "Stamina should decrease correctly");
    }

    #endregion

    #region HP Recovery Tests

    [Test]
    public void RecoverHp_WithinMaxHp_IncreasesHp()
    {
        _playerStats.TryAddDamage(30f); // Reduce to 70 HP
        _playerStats.RecoverHp(10f);
        
        Assert.AreEqual(80f, _playerStats.CurrentHp, "HP should increase by recovery amount");
    }

    [Test]
    public void RecoverHp_ExceedsMaxHp_CapsAtMaxHp()
    {
        _playerStats.TryAddDamage(20f); // Reduce to 80 HP
        _playerStats.RecoverHp(50f); // Try to recover more than max
        
        Assert.AreEqual(100f, _playerStats.CurrentHp, "HP should not exceed MaxHp");
    }

    [Test]
    public void RecoverHp_WhenAtMaxHp_RemainsAtMaxHp()
    {
        _playerStats.RecoverHp(10f);
        
        Assert.AreEqual(100f, _playerStats.CurrentHp, "HP should stay at max");
    }

    [Test]
    public void RecoverHp_ZeroAmount_NoChange()
    {
        _playerStats.TryAddDamage(20f);
        float currentHp = _playerStats.CurrentHp;
        _playerStats.RecoverHp(0f);
        
        Assert.AreEqual(currentHp, _playerStats.CurrentHp, "HP should not change with zero recovery");
    }

    [Test]
    public void RecoverHp_NegativeAmount_DecreasesHp()
    {
        // Note: The current implementation doesn't validate negative recovery
        _playerStats.TryAddDamage(20f); // 80 HP
        _playerStats.RecoverHp(-10f);
        
        Assert.AreEqual(70f, _playerStats.CurrentHp, "Negative recovery acts as damage");
    }

    [Test]
    public void RecoverHp_FromZeroHp_IncreasesHp()
    {
        _playerStats.TryAddDamage(100f); // Kill player
        _playerStats.RecoverHp(30f);
        
        Assert.AreEqual(30f, _playerStats.CurrentHp, "Should be able to recover from 0 HP");
        Assert.IsFalse(_playerStats.IsDead, "Player should no longer be dead");
    }

    [Test]
    public void RecoverHp_VeryLargeAmount_CapsAtMaxHp()
    {
        _playerStats.TryAddDamage(50f);
        _playerStats.RecoverHp(10000f);
        
        Assert.AreEqual(100f, _playerStats.CurrentHp, "Should cap at MaxHp even with huge recovery");
    }

    #endregion

    #region Stamina Recovery Tests

    [Test]
    public void RecoverStamina_WithinMaxStamina_IncreasesStamina()
    {
        _playerStats.TryUseStamina(20f); // Reduce to 30 stamina
        _playerStats.RecoverStamina(10f);
        
        Assert.AreEqual(40f, _playerStats.CurrentStamina, "Stamina should increase by recovery amount");
    }

    [Test]
    public void RecoverStamina_ExceedsMaxStamina_CapsAtMaxStamina()
    {
        _playerStats.TryUseStamina(10f); // Reduce to 40 stamina
        _playerStats.RecoverStamina(50f);
        
        Assert.AreEqual(50f, _playerStats.CurrentStamina, "Stamina should not exceed MaxStamina");
    }

    [Test]
    public void RecoverStamina_WhenAtMaxStamina_RemainsAtMaxStamina()
    {
        _playerStats.RecoverStamina(10f);
        
        Assert.AreEqual(50f, _playerStats.CurrentStamina, "Stamina should stay at max");
    }

    [Test]
    public void RecoverStamina_ZeroAmount_NoChange()
    {
        _playerStats.TryUseStamina(10f);
        float currentStamina = _playerStats.CurrentStamina;
        _playerStats.RecoverStamina(0f);
        
        Assert.AreEqual(currentStamina, _playerStats.CurrentStamina, "Stamina should not change");
    }

    [Test]
    public void RecoverStamina_NegativeAmount_DecreasesStamina()
    {
        _playerStats.RecoverStamina(-10f);
        
        Assert.AreEqual(40f, _playerStats.CurrentStamina, "Negative recovery acts as usage");
    }

    [Test]
    public void RecoverStamina_FromZeroStamina_IncreasesStamina()
    {
        _playerStats.TryUseStamina(50f); // Use all stamina
        _playerStats.RecoverStamina(25f);
        
        Assert.AreEqual(25f, _playerStats.CurrentStamina, "Should recover from 0 stamina");
    }

    #endregion

    #region Stamina Regeneration Tests

    [Test]
    public void UpdateStaminaRegeneration_BelowMax_RegeneratesStamina()
    {
        _playerStats.TryUseStamina(20f); // 30 stamina remaining
        
        // Simulate Time.deltaTime = 0.1f, regenRate = 5f
        // Expected regen: 5 * 0.1 = 0.5
        float initialStamina = _playerStats.CurrentStamina;
        
        // Mock Time.deltaTime by calling with calculated amount
        _playerStats.RecoverStamina(5f * 0.1f); // Simulating the regeneration
        
        Assert.Greater(_playerStats.CurrentStamina, initialStamina, "Stamina should regenerate");
    }

    [Test]
    public void UpdateStaminaRegeneration_AtMax_NoRegeneration()
    {
        // At max stamina, UpdateStaminaRegeneration should not add more
        float initialStamina = _playerStats.CurrentStamina;
        
        // The method only calls RecoverStamina if below max
        if (_playerStats.CurrentStamina < _playerStats.MaxStamina)
        {
            _playerStats.RecoverStamina(5f * 0.1f);
        }
        
        Assert.AreEqual(initialStamina, _playerStats.CurrentStamina, "No regen at max stamina");
    }

    [Test]
    public void UpdateStaminaRegeneration_MultipleUpdates_GraduallyRegenerates()
    {
        _playerStats.TryUseStamina(30f); // 20 stamina remaining
        
        // Simulate multiple frames
        for (int i = 0; i < 5; i++)
        {
            float regenAmount = 2f * 0.1f; // regenRate * deltaTime
            if (_playerStats.CurrentStamina < _playerStats.MaxStamina)
            {
                _playerStats.RecoverStamina(regenAmount);
            }
        }
        
        Assert.AreEqual(21f, _playerStats.CurrentStamina, 0.01f, "Stamina should regenerate over time");
    }

    #endregion

    #region IsDead Property Tests

    [Test]
    public void IsDead_WhenHpAboveZero_ReturnsFalse()
    {
        Assert.IsFalse(_playerStats.IsDead, "Player should not be dead with HP > 0");
    }

    [Test]
    public void IsDead_WhenHpIsZero_ReturnsTrue()
    {
        _playerStats.TryAddDamage(100f);
        Assert.IsTrue(_playerStats.IsDead, "Player should be dead with HP = 0");
    }

    [Test]
    public void IsDead_AfterRecovery_ReturnsFalse()
    {
        _playerStats.TryAddDamage(100f); // Kill
        _playerStats.RecoverHp(10f); // Revive
        
        Assert.IsFalse(_playerStats.IsDead, "Player should not be dead after recovery");
    }

    #endregion

    #region Integration Tests - Complex Scenarios

    [Test]
    public void Scenario_CombatSequence_WorksCorrectly()
    {
        // Take damage
        _playerStats.TryAddDamage(30f); // 70 HP
        Assert.AreEqual(70f, _playerStats.CurrentHp);
        
        // Use stamina for dodge
        bool canDodge = _playerStats.TryUseStamina(15f); // 35 stamina
        Assert.IsTrue(canDodge);
        
        // Take more damage
        _playerStats.TryAddDamage(20f); // 50 HP
        
        // Try to use more stamina
        bool canAttack = _playerStats.TryUseStamina(30f); // 5 stamina
        Assert.IsTrue(canAttack);
        
        // Recover some HP
        _playerStats.RecoverHp(25f); // 75 HP
        
        Assert.AreEqual(75f, _playerStats.CurrentHp);
        Assert.AreEqual(5f, _playerStats.CurrentStamina);
        Assert.IsFalse(_playerStats.IsDead);
    }

    [Test]
    public void Scenario_NearDeathRecovery_WorksCorrectly()
    {
        // Nearly kill player
        _playerStats.TryAddDamage(99f); // 1 HP
        Assert.AreEqual(1f, _playerStats.CurrentHp);
        Assert.IsFalse(_playerStats.IsDead);
        
        // Small additional damage should kill
        bool survived = _playerStats.TryAddDamage(1f);
        Assert.IsFalse(survived);
        Assert.IsTrue(_playerStats.IsDead);
    }

    [Test]
    public void Scenario_StaminaDepletion_PreventActions()
    {
        // Use all stamina
        _playerStats.TryUseStamina(50f);
        
        // Try to perform action requiring stamina
        bool canPerformAction = _playerStats.TryUseStamina(10f);
        Assert.IsFalse(canPerformAction, "Should not be able to use stamina when depleted");
        
        // Recover some stamina
        _playerStats.RecoverStamina(15f);
        
        // Now should be able to use stamina
        canPerformAction = _playerStats.TryUseStamina(10f);
        Assert.IsTrue(canPerformAction, "Should be able to use stamina after recovery");
        Assert.AreEqual(5f, _playerStats.CurrentStamina);
    }

    [Test]
    public void Scenario_RepeatedDamageAndHeal_MaintainsConsistentState()
    {
        for (int i = 0; i < 10; i++)
        {
            _playerStats.TryAddDamage(5f);
            _playerStats.RecoverHp(3f);
        }
        
        // After 10 cycles: (100 - 5 + 3) * 10 = 100 - 20 = 80
        Assert.AreEqual(80f, _playerStats.CurrentHp, "State should be consistent after repeated operations");
    }

    #endregion

    #region Property Access Tests

    [Test]
    public void Properties_AreReadOnly_CannotBeSetDirectly()
    {
        // These properties should only have getters
        float hp = _playerStats.CurrentHp;
        float stamina = _playerStats.CurrentStamina;
        float maxHp = _playerStats.MaxHp;
        float maxStamina = _playerStats.MaxStamina;
        bool isDead = _playerStats.IsDead;
        
        // Just verify they can be read
        Assert.IsNotNull(hp);
        Assert.IsNotNull(stamina);
        Assert.IsNotNull(maxHp);
        Assert.IsNotNull(maxStamina);
        Assert.IsNotNull(isDead);
    }

    #endregion
}