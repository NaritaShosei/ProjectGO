using System;
using UnityEngine;

public class PlayerStats
{
    private float _maxHp;
    private float _maxStamina;
    private float _currentHp;
    private float _currentStamina;

    public bool IsDead => _currentHp <= 0;

    /// <summary>
    /// 第一引数現在の値、第二引数最大値
    /// </summary>
    public event Action<float, float> OnHealthChange;

    /// <summary>
    /// 第一引数現在の値、第二引数最大値
    /// </summary>
    public event Action<float, float> OnStaminaChange;

    public PlayerStats(CharacterData data)
    {
        _maxHp = data.MaxHP;
        _maxStamina = data.MaxStamina;
        _currentHp = _maxHp;
        _currentStamina = _maxStamina;
    }

    /// <summary>
    /// ダメージを受ける。HPが0になったらfalseを返す
    /// </summary>
    /// <param name="damage">ダメージ量</param>
    public bool TryAddDamage(float damage)
    {
        if (damage < 0)
        {
            Debug.LogWarning("ダメージは0以上である必要があります");
            return !IsDead;
        }

        _currentHp = Mathf.Max(0, _currentHp - damage);

        OnHealthChange?.Invoke(_currentHp, _maxHp);

        Debug.Log($"現在のHP:{_currentHp}");
        return !IsDead;
    }

    /// <summary>
    /// スタミナを消費する。消費できた場合はtrueを返す
    /// </summary>
    /// <param name="amount">消費する量</param>
    public bool TryUseStamina(float amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("スタミナ消費量は0以上である必要があります");
            return false;
        }

        if (_currentStamina < amount)
        {
            return false;
        }

        _currentStamina -= amount;

        OnStaminaChange?.Invoke(_currentStamina, _maxStamina);

        Debug.Log($"現在のスタミナ:{_currentStamina}");
        return true;
    }

    /// <summary>
    /// HPを回復する
    /// </summary>
    /// <param name="amount">回復する量</param>
    public void RecoverHp(float amount)
    {
        _currentHp = Mathf.Min(_maxHp, _currentHp + amount);

        OnHealthChange?.Invoke(_currentHp, _maxHp);
    }

    /// <summary>
    /// スタミナを回復する
    /// </summary>
    /// <param name="amount">回復する量</param>
    public void RecoverStamina(float amount)
    {
        _currentStamina = Mathf.Min(_maxStamina, _currentStamina + amount);
        OnStaminaChange?.Invoke(_currentStamina, _maxStamina);
    }


    /// <summary>
    /// 時間経過でスタミナを自動回復
    /// </summary>
    /// <param name="regenRate">一秒間に回復する量</param>
    public void UpdateStaminaRegeneration(float regenRate)
    {
        if (_currentStamina < _maxStamina)
        {
            RecoverStamina(regenRate * Time.deltaTime);
        }
    }
}