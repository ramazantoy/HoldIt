using AF.Audio;
using UnityEngine;
using VContainer.Unity;

namespace HoldIt.Feedback
{
    /// <summary>
    /// Starts a scene's music track when the scene starts. The track crossfades from the previous scene's and goes on
    /// across scene changes, so a scene without music of its own leaves the last one playing. No clip, no music.
    /// </summary>
    internal sealed class SceneMusic : IStartable
    {
        private readonly IMusic _music;
        private readonly AudioClip _clip;

        public SceneMusic(IMusic music, AudioClip clip)
        {
            _music = music;
            _clip = clip;
        }

        public void Start()
        {
            if (_clip != null)
                _music.Play(_clip);
        }
    }
}
