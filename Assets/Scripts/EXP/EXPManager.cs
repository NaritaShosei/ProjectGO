using System;
using UnityEngine;

public class EXPManager : MonoBehaviour
{
    /// <summary>
    /// レベルアップイベント。レベルが上がるたびに、現在のレベルを引数として発火する。
    /// </summary>
    public event Action<int> OnLevelUp;

    /// <summary>
    /// 経験値追加イベント。経験値が追加されるたびに、追加された経験値の量、現在のレベル、現在の経験値を引数として発火する。
    /// </summary>
    public event Action<AddEXPContext> OnAddEXP;

    /// <summary>
    /// 経験値を追加するメソッド。経験値がレベルアップに必要な量を超えると、レベルアップ処理が自動的に行われる。
    /// </summary>
    public void AddEXP(float amount)
    {
        _currentEXP += amount;
        CheckLevelUp();
        OnAddEXP?.Invoke(new AddEXPContext(amount, _currentLevel, _currentEXP));
    }

    [SerializeField] private float _levelUpEXP = 100f;
    private float _currentEXP = 0;
    private int _currentLevel = 1;

    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<EXPManager>();
    }

    /// <summary>
    /// 現在の経験値がレベルアップに必要な経験値を超えているかをチェックし、超えている場合はレベルアップ処理を行う。
    /// </summary>
    private void CheckLevelUp()
    {
        // 経験値が足りている間、繰り返しレベルアップ処理を行う
        while (_currentEXP >= _levelUpEXP)
        {
            _currentEXP -= _levelUpEXP;
            LevelUp();
        }
    }

    /// <summary>
    /// レベルアップ処理を行うメソッド。レベルアップイベントを発火させる。
    /// </summary>
    private void LevelUp()
    {
        _currentLevel++;
        OnLevelUp?.Invoke(_currentLevel);
        Debug.Log($"レベルアップ！現在のレベル: {_currentLevel}");
    }
}

public readonly struct AddEXPContext
{
    public readonly float Amount;
    public readonly int CurrentLevel;
    public readonly float CurrentEXP;

    public AddEXPContext(float amount, int currentLevel, float currentEXP)
    {
        Amount = amount;
        CurrentLevel = currentLevel;
        CurrentEXP = currentEXP;
    }
}
