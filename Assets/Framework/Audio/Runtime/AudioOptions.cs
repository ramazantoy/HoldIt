using System;
using System.Collections.Generic;

namespace AF.Audio
{
    /// <summary>Passed to <c>builder.UseAudio(mixer, messagePipe, options)</c> in the root scope.</summary>
    public sealed class AudioOptions
    {
        private readonly List<AudioChannel> _channels = new List<AudioChannel>();

        /// <summary>The channel whose mixer group the music plays through; one of <see cref="Channels"/>.</summary>
        public AudioChannel Music { get; set; }

        /// <summary>The channels whose volume can be set, in the order a settings screen lists them.</summary>
        public IReadOnlyList<AudioChannel> Channels => _channels;

        /// <summary>Sound sources created when the root scope is built; the pool grows past it when needed.</summary>
        public int PrewarmVoices { get; set; } = 8;

        /// <summary>Silences the game while its window is not focused. Desktop players only: the Editor is often unfocused while it
        /// runs, and on mobile the on-screen keyboard and system overlays take the focus.</summary>
        public bool MuteWhenUnfocused { get; set; } = true;

        public AudioOptions Add(AudioChannel channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (!_channels.Contains(channel))
                _channels.Add(channel);
            return this;
        }
    }
}
