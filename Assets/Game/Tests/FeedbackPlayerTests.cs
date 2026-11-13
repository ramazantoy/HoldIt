using System.Collections.Generic;
using AF.Audio;
using HoldIt.Feedback;
using HoldIt.Haptics;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class FeedbackPlayerTests
    {
        private SoundCue _cue;
        private RecordingSounds _sounds;
        private RecordingSounds _globalSounds;
        private RecordingHaptics _haptics;
        private FakeClock _clock;

        [SetUp]
        public void SetUpPlayer()
        {
            _cue = ScriptableObject.CreateInstance<SoundCue>();
            _sounds = new RecordingSounds();
            _globalSounds = new RecordingSounds();
            _haptics = new RecordingHaptics();
            _clock = new FakeClock { Time = 10f };
        }

        [TearDown]
        public void TearDownPlayer() => Object.DestroyImmediate(_cue);

        private FeedbackPlayer PlayerWith(params FeedbackEntry[] entries) =>
            new FeedbackPlayer(new FeedbackTable(entries), _sounds, _globalSounds, _haptics, _clock);

        [Test]
        public void Play_AKindWithoutAnEntry_MakesNoSoundAndNoBuzz()
        {
            var player = PlayerWith(new FeedbackEntry(FeedbackKind.Served, _cue, HapticStrength.Light));

            player.Play(FeedbackKind.Accident);

            Assert.IsEmpty(_sounds.Played);
            Assert.IsEmpty(_globalSounds.Played);
            Assert.IsEmpty(_haptics.Played);
        }

        [Test]
        public void Play_ASceneBoundCue_GoesToTheSceneSoundsWithItsVibration()
        {
            var player = PlayerWith(new FeedbackEntry(FeedbackKind.Served, _cue, HapticStrength.Light));

            player.Play(FeedbackKind.Served);

            CollectionAssert.AreEqual(new[] { _cue }, _sounds.Played);
            Assert.IsEmpty(_globalSounds.Played);
            CollectionAssert.AreEqual(new[] { HapticStrength.Light }, _haptics.Played);
        }

        [Test]
        public void Play_ACueThatOutlivesTheScene_GoesToTheGlobalSounds()
        {
            var player = PlayerWith(new FeedbackEntry(FeedbackKind.UiClick, _cue, outlivesScene: true));

            player.Play(FeedbackKind.UiClick);

            CollectionAssert.AreEqual(new[] { _cue }, _globalSounds.Played);
            Assert.IsEmpty(_sounds.Played);
        }

        [Test]
        public void Play_WithoutACue_StillVibrates()
        {
            var player = PlayerWith(new FeedbackEntry(FeedbackKind.Accident, haptic: HapticStrength.Heavy));

            player.Play(FeedbackKind.Accident);

            Assert.IsEmpty(_sounds.Played);
            Assert.IsEmpty(_globalSounds.Played);
            CollectionAssert.AreEqual(new[] { HapticStrength.Heavy }, _haptics.Played);
        }

        [Test]
        public void Play_AgainTooSoon_IsDroppedWholly()
        {
            var player = PlayerWith(new FeedbackEntry(FeedbackKind.PathCell, _cue, HapticStrength.Tick, 0.5f));

            player.Play(FeedbackKind.PathCell);
            _clock.Time += 0.1f;
            player.Play(FeedbackKind.PathCell);

            Assert.AreEqual(1, _sounds.Played.Count);
            Assert.AreEqual(1, _haptics.Played.Count);
        }

        private sealed class RecordingSounds : IGlobalSounds
        {
            public readonly List<SoundCue> Played = new List<SoundCue>();

            public SoundHandle Play(SoundCue cue)
            {
                Played.Add(cue);
                return default;
            }

            public SoundHandle PlayAt(SoundCue cue, Vector3 position) => Play(cue);
        }

        private sealed class RecordingHaptics : IHaptics
        {
            public readonly List<HapticStrength> Played = new List<HapticStrength>();

            public void Play(HapticStrength strength) => Played.Add(strength);
        }
    }
}
