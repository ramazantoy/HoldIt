namespace HoldIt.Settings
{
    /// <summary>The game's own settings, saved with the global data. Read where they apply; the settings screen sets them.</summary>
    public interface IGameSettings
    {
        /// <summary>Whether the game may vibrate. The vibrations themselves come with the juice pass (M9).</summary>
        bool Haptics { get; }

        void SetHaptics(bool on);
    }
}
