using UnityEngine;

public class ResultPanelModel
{
    public float BossClearTime => _result.BossClearTime;
    public int Level => _result.Level;
    public int Score { get; }

    public ResultRank GetRank(int sMinimumScore, int aMinimumScore, int bMinimumScore)
    {
        // 閾値が逆転しても上位ランクほど低い点数にならないよう補正する。
        int bThreshold = Mathf.Max(0, bMinimumScore);
        int aThreshold = Mathf.Max(bThreshold, aMinimumScore);
        int sThreshold = Mathf.Max(aThreshold, sMinimumScore);
        if (Score >= sThreshold) return ResultRank.S;
        if (Score >= aThreshold) return ResultRank.A;
        if (Score >= bThreshold) return ResultRank.B;
        return ResultRank.C;
    }

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
