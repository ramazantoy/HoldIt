using System;
using AF.Core;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace AF.Audio
{
    /// <summary>
    /// A sound as the designer sets it up: its clips, the mixer group it plays through and how it plays. Code plays it with
    /// <see cref="ISounds"/> and never names a mixer group. Validation reports a cue without clips or without an output.
    /// </summary>
    [CreateAssetMenu(menuName = FrameworkConstants.MenuRoot + "Sound Cue", fileName = "SoundCue")]
    public sealed class SoundCue : ScriptableObject
    {
        [SerializeField, Tooltip("One is picked at random on every play.")]
        private AudioClip[] _clips = Array.Empty<AudioClip>();

        [SerializeField, Required, Tooltip("The mixer group it plays through; that group's channel volume applies.")]
        private AudioMixerGroup _output;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        [SerializeField, Tooltip("A pitch is picked between the two on every play.")]
        private Vector2 _pitchRange = Vector2.one;

        [SerializeField, Tooltip("Plays until its handle stops it or its scope ends.")]
        private bool _loop;

        [SerializeField, Tooltip("Keeps playing while the game is paused, and fades in unscaled time: interface sounds.")]
        private bool _playWhilePaused;

        [Header("3D (PlayAt)")]
        [SerializeField, Min(0f)]
        private float _minDistance = 1f;

        [SerializeField, Min(0f)]
        private float _maxDistance = 30f;

        [SerializeField]
        private AudioRolloffMode _rolloff = AudioRolloffMode.Logarithmic;

        public AudioMixerGroup Output => _output;
        public float Volume => _volume;
        public bool Loop => _loop;
        public bool PlayWhilePaused => _playWhilePaused;
        public float MinDistance => _minDistance;
        public float MaxDistance => _maxDistance;
        public AudioRolloffMode Rolloff => _rolloff;
        internal AudioClip[] Clips => _clips;

        /// <summary>A cue made in code, for tests.</summary>
        internal static SoundCue Create(AudioMixerGroup output, bool loop = false, bool playWhilePaused = false, params AudioClip[] clips)
        {
            var cue = CreateInstance<SoundCue>();
            cue._output = output;
            cue._loop = loop;
            cue._playWhilePaused = playWhilePaused;
            cue._clips = clips;
            return cue;
        }

        internal AudioClip PickClip() => _clips.Length == 0 ? null : _clips[Random.Range(0, _clips.Length)];

        internal float PickPitch() => Random.Range(_pitchRange.x, _pitchRange.y);
    }
}
