namespace AF.Settings
{
    /// <summary>How the game window covers the screen. Saved by name: renaming a member needs a migration step.</summary>
    public enum WindowMode
    {
        /// <summary>A window with borders, smaller than the screen.</summary>
        Windowed,

        /// <summary>A borderless window that covers the screen (Unity's <c>FullScreenWindow</c>).</summary>
        Borderless,

        /// <summary>Exclusive fullscreen; Windows only.</summary>
        Exclusive,
    }
}
