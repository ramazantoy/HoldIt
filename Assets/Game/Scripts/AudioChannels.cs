using AF.Audio;

namespace Game
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

        public static AudioOptions CreateOptions() =>
            new AudioOptions { Music = Music }
                .Add(Master)
                .Add(Music)
                .Add(Sfx)
                .Add(Ui);
    }
}
