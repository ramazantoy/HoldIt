using UnityEngine;

namespace AF.Audio
{
    /// <summary>
    /// The one music track of the game. A new track crossfades with the current one; the last call wins. Music keeps playing
    /// while the game is paused and across scene changes, and fades in unscaled time.
    /// </summary>
    public interface IMusic
    {
        /// <summary>The track playing or fading in; null after <see cref="Stop"/>.</summary>
        AudioClip Current { get; }

        /// <summary>Crossfades to <paramref name="clip"/>, looped. Does nothing when it is already the current track.</summary>
        void Play(AudioClip clip, float fadeSeconds = 1f);

        void Stop(float fadeSeconds = 1f);
    }
}
