using System;

namespace HoldIt.Settings
{
    /// <summary>
    /// The settings screen of a scene: the menu opens it from the main menu, the gameplay scene from the pause screen.
    /// Whoever opens it shows its own screen again on <see cref="Closed"/>.
    /// </summary>
    public interface ISettingsScreen
    {
        bool IsOpen { get; }

        event Action Closed;

        /// <summary>Shows the current settings. Does nothing while open.</summary>
        void Open();

        /// <summary>Hides the screen and writes the settings to disk. Does nothing while closed.</summary>
        void Close();
    }
}
