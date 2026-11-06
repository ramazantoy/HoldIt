using System;
using AF.Audio;
using HoldIt.Haptics;
using UnityEngine;

namespace HoldIt.Feedback
{
    /// <summary>What one kind of event sounds and feels like. An empty cue is a kind that makes no sound (yet).</summary>
    [Serializable]
    public sealed class FeedbackEntry
    {
        [SerializeField] private FeedbackKind _kind;

        [SerializeField, Tooltip("The sound; empty for none.")]
        private SoundCue _cue;

        [SerializeField, Tooltip("The vibration.")]
        private HapticStrength _haptic;

        [SerializeField, Min(0f), Tooltip("The shortest time between two of these, in game seconds; one asked for sooner is dropped. 0 for no limit.")]
        private float _minInterval;

        [SerializeField, Tooltip("The sound goes on when the scene changes (a button that loads one). Otherwise it stops with the scene.")]
        private bool _outlivesScene;

        public FeedbackEntry(FeedbackKind kind, SoundCue cue = null, HapticStrength haptic = HapticStrength.None,
            float minInterval = 0f, bool outlivesScene = false)
        {
            _kind = kind;
            _cue = cue;
            _haptic = haptic;
            _minInterval = minInterval;
            _outlivesScene = outlivesScene;
        }

        public FeedbackKind Kind => _kind;
        public SoundCue Cue => _cue;
        public HapticStrength Haptic => _haptic;
        public float MinInterval => _minInterval;
        public bool OutlivesScene => _outlivesScene;
    }
}
