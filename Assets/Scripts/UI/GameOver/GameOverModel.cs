public sealed class GameOverModel
{
    public GameOverModel(GameOverReason reason) => Reason = reason;

    public GameOverReason Reason { get; }
    public string DisplayText => "GAME OVER";
}
