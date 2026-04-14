public interface ISkillUpdater
{
    //一定間隔で使用するスキルにつける
    void OnUpdate(float deltaTime, PlayerMode mode, IPlayerStats stats);
}
