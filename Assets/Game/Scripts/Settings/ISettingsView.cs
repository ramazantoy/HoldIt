using System;

namespace HoldIt.Settings
{
    /// <summary>
    /// The settings screen's controls: a volume slider per row of <see cref="AudioChannels.SettingsSliders"/>, the
    /// vibration switch and the language. Setting a value from code raises no event.
    /// </summary>
    public interface ISettingsView
    {
        /// <summary>A slider moved: its row and its value, 0 to 1.</summary>
        event Action<int, float> VolumeChanged;

        event Action<bool> HapticsChanged;

        /// <summary>A language arrow: -1 for the previous language, +1 for the next.</summary>
        event Action<int> LanguageStepped;

        event Action BackClicked;

        int VolumeCount { get; }

        void SetVolume(int row, float volume);
        void SetHaptics(bool on);
        void SetLanguage(string name);

        void Show();
        void Hide();
    }
}
