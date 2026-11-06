using AF.Audio;
using UnityEngine;
using VContainer.Unity;

namespace HoldIt.Feedback
{
    /// <summary>
    /// Starts a scene's music track when the scene starts, at the scene's own level. The level is set on the music bed
    /// (<see cref="AudioChannels.MusicBed"/>), which sits under the Music channel: the player's slider tops out at 1 but a
    /// track that is too loud can be turned down for good here. The track crossfades from the previous scene's and goes on
    /// across scene changes, so a scene without music of its own leaves the last one playing. No clip, no music.
    /// </summary>
    internal sealed class SceneMusic : IStartable
    {
        private readonly IMusic _music;
        private readonly IAudioVolumes _volumes;
        private readonly AudioClip _clip;
        private readonly float _level;

        public SceneMusic(IMusic music, IAudioVolumes volumes, AudioClip clip, float level)
        {
            _music = music;
            _volumes = volumes;
            _clip = clip;
            _level = level;
        }

        public void Start()
        {
            if (_clip == null)
                return;
            _volumes.Set(AudioChannels.MusicBed, _level);
            _music.Play(_clip);
        }
    }
}
