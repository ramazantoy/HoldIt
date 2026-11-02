namespace HoldIt.Board
{
    /// <summary>Screen space the board must leave free, such as the HUD's top bar.</summary>
    public interface IBoardInsets
    {
        /// <summary>Pixels from the top of the screen that the board stays below.</summary>
        float Top { get; }
    }
}
