using System.Collections.Generic;
using AF.Audio;

namespace HoldIt
{
    /// <summary>
    /// The game's volume channels: the groups of Assets/Game/Audio/GameMixer.mixer and the exposed parameters of their volume.
    /// Add a channel here when the mixer gets a group whose volume the player sets.
    /// </summary>
    public static class AudioChannels
    {
        public static readonly AudioChannel Master = new AudioChannel("Master", "MasterVolume");
        public static readonly AudioChannel Music = new AudioChannel("Music", "MusicVolume");
        public static readonly AudioChannel Sfx = new AudioChannel("SFX", "SfxVolume");
        public static readonly AudioChannel Ui = new AudioChannel("UI", "UiVolume");

        /// <summary>
        /// The settings screen's volume sliders, top to bottom; the screen's prefab has one slider per row. The UI channel
        /// is not among them: it follows Master.
        /// </summary>
        public static readonly IReadOnlyList<AudioChannel> SettingsSliders = new[] { Master, Music, Sfx };

        public static AudioOptions CreateOptions() =>
            new AudioOptions { Music = Music }
                .Add(Master)
                .Add(Music)
                .Add(Sfx)
                .Add(Ui);
    }
}
