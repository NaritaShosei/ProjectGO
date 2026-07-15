using UnityEngine;

public class ResultPanelModel
{
    public float BossClearTime => _result.BossClearTime;
    public int Level => _result.Level;
    public int Score { get; }

    public ResultPanelModel(
        ResultData result,
        int baseScore,
        float timeScorePerSecond,
        int levelScoreMultiplier)
    {
        _result = result;

        float remainingTime = Mathf.Max(0f, result.BossBattleTimeLimit - result.BossClearTime);
        int timeScore = Mathf.RoundToInt(remainingTime * timeScorePerSecond);
        int levelScore = Mathf.Max(0, result.Level - 1) * levelScoreMultiplier;

        Score = Mathf.Max(0, baseScore + timeScore + levelScore);
    }

    private readonly ResultData _result;
}
