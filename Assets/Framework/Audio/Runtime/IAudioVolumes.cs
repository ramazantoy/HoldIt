using System;
using System.Collections.Generic;

namespace AF.Audio
{
    /// <summary>
    /// Channel volumes and muting. Volumes are linear, 0 to 1, as a slider shows them; they reach the mixer in decibels. The
    /// Settings module saves them and sets them back at startup. The Audio module is the only writer of
    /// <c>AudioListener.pause</c> (mirrors <c>IPauseService</c>) and <c>AudioListener.volume</c> (mute).
    /// </summary>
    public interface IAudioVolumes
    {
        IReadOnlyList<AudioChannel> Channels { get; }

        float Get(AudioChannel channel);

        /// <summary>Sets the channel's volume, clamped to 0–1, and publishes <see cref="AudioVolumeChanged"/> if it changed.
        /// Throws for a channel not in the options.</summary>
        void Set(AudioChannel channel, float volume);

        /// <summary>Silences everything, music included, until the handle is disposed (an ad, a video). Like pause requests,
        /// the game stays silent while any handle is alive.</summary>
        IDisposable Mute(string reason);

        /// <summary>Silences the game while its window is not focused (desktop players only). Starts from <see cref="AudioOptions.MuteWhenUnfocused"/>.</summary>
        bool MuteWhenUnfocused { get; set; }
    }
}
