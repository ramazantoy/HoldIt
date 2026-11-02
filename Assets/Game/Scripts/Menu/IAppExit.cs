namespace HoldIt.Menu
{
    /// <summary>Leaving the game. Mobile players leave with the back button, so only desktop shows a Quit button.</summary>
    public interface IAppExit
    {
        bool HasQuitButton { get; }

        void Quit();
    }
}
