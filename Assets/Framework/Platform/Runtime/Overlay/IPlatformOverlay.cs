namespace AF.Platform
{
    /// <summary>
    /// The store's in-game overlay (Steam, Epic). <see cref="PlatformOverlayChanged"/> follows it. With
    /// <see cref="PlatformOptions.PauseOnOverlay"/> the game is paused while it is open; the game may also open its own pause
    /// menu on the message, so closing the overlay does not throw the player back into gameplay.
    /// </summary>
    public interface IPlatformOverlay
    {
        bool IsOpen { get; }
    }
}
