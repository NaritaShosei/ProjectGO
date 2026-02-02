namespace Interface
{
    public interface IPlayerModeView

    {
        /// <summary>
        /// プレイヤーモードの表示切り替え
        /// </summary>
        /// <param name="mode"></param>
        void SetMode(PlayerMode mode);
    }
}
