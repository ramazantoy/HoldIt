using PrimeTween;
using UnityEngine;

namespace AF.Audio
{
    /// <summary>A pooled sound source: one play of a <see cref="SoundCue"/> per life.</summary>
    [AddComponentMenu(""), RequireComponent(typeof(AudioSource))]
    internal sealed class SoundVoice : MonoBehaviour
    {
        private AudioSource _source;

        public AudioSource Source
        {
            get
            {
                if (_source == null)
                    _source = GetComponent<AudioSource>();
                return _source;
            }
        }

        /// <summary>The fade-out of the current life, if one runs.</summary>
        public Tween Fade { get; set; }

        /// <summary>Stopped with a fade: the voice goes back once the fade is over.</summary>
        public bool Stopping { get; set; }
    }
}
