using AF.Core;

namespace AF.Audio
{
    /// <summary>
    /// One play of a sound. Once the sound ends, the handle stays dead even when its source plays another sound, so holding
    /// it is safe. A default handle is dead.
    /// </summary>
    public readonly struct SoundHandle
    {
        private readonly VoicePlayer _player;
        private readonly SoundVoice _voice;
        private readonly PoolLife _life;

        internal SoundHandle(VoicePlayer player, SoundVoice voice, PoolLife life)
        {
            _player = player;
            _voice = voice;
            _life = life;
        }

        internal SoundVoice Voice => _voice;

        /// <summary>True until the sound ends, is stopped, or its scope ends.</summary>
        public bool IsPlaying => _life.IsCurrent;

        /// <summary>Stops the sound, fading it out over <paramref name="fadeSeconds"/>. Does nothing once the sound has ended.</summary>
        public void Stop(float fadeSeconds = 0f) => _player?.Stop(_voice, _life, fadeSeconds);
    }
}
