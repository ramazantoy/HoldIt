using System;

namespace HoldIt.Menu
{
    /// <summary>The settings screen. Empty until M7 (sound, haptics, language).</summary>
    public interface ISettingsView
    {
        event Action BackClicked;

        void Show();
        void Hide();
    }
}
