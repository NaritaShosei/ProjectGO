public readonly struct ResultData
{
    public float BossClearTime { get; }
    public float BossBattleTimeLimit { get; }
    public int Level { get; }

    public ResultData(float bossClearTime, float bossBattleTimeLimit, int level)
    {
        BossClearTime = bossClearTime;
        BossBattleTimeLimit = bossBattleTimeLimit;
        Level = level;
    }
}
