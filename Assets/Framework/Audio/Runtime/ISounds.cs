using UnityEngine;

namespace AF.Audio
{
    /// <summary>
    /// Plays sound cues. Every scope resolves its own: sounds played through a scene's instance stop when that scene's scope
    /// ends (under the loading screen); sounds played through the root's survive scene changes. Scene code that needs the
    /// root's asks for <see cref="IGlobalSounds"/>.
    /// </summary>
    public interface ISounds
    {
        /// <summary>Plays the cue without position (2D).</summary>
        SoundHandle Play(SoundCue cue);

        /// <summary>Plays the cue at a world position (3D), with the cue's distances and rolloff.</summary>
        SoundHandle PlayAt(SoundCue cue, Vector3 position);
    }
}
