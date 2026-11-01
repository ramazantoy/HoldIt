namespace AF.Settings
{
    /// <summary>
    /// Passed to <c>builder.UseSettings(messagePipe, options)</c> in the root scope: the defaults a player who never changed a
    /// setting gets. They are not saved, so a later build's defaults reach those players too.
    /// </summary>
    public sealed class SettingsOptions
    {
        public WindowMode WindowMode { get; set; } = WindowMode.Borderless;

        public bool VSync { get; set; } = true;

        /// <summary>Frames per second on desktop platforms; 0 is no limit. Ignored while vsync is on.</summary>
        public int FrameRateLimit { get; set; }

        /// <summary>Frames per second on mobile; 0 is the display's refresh rate. Unity's own mobile default is 30.</summary>
        public int MobileFrameRateLimit { get; set; } = 60;

        public bool ConfineCursor { get; set; }
    }
}
