using System.Collections.Generic;
using HoldIt.Haptics;
using HoldIt.Settings;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class HapticsTests
    {
        private RecordingDevice _device;
        private FakeGameSettings _settings;
        private FakeClock _clock;
        private HapticsConfig _config;
        private HapticPlayer _haptics;

        [SetUp]
        public void SetUpHaptics()
        {
            _device = new RecordingDevice();
            _settings = new FakeGameSettings { Haptics = true };
            _clock = new FakeClock { Time = 10f };
            _config = ScriptableObject.CreateInstance<HapticsConfig>();
            _haptics = new HapticPlayer(_device, _settings, _config, _clock);
        }

        [TearDown]
        public void TearDownHaptics() => Object.DestroyImmediate(_config);

        [Test]
        public void Play_WhenAllowed_VibratesWithThatStrength()
        {
            _haptics.Play(HapticStrength.Light);

            CollectionAssert.AreEqual(new[] { HapticStrength.Light }, _device.Played);
        }

        [Test]
        public void Play_WithVibrationOff_DoesNothing()
        {
            _settings.Haptics = false;

            _haptics.Play(HapticStrength.Heavy);

            Assert.IsEmpty(_device.Played);
        }

        [Test]
        public void Play_SwitchedOnLater_VibratesFromThen()
        {
            _settings.Haptics = false;
            _haptics.Play(HapticStrength.Tick);

            _settings.Haptics = true;
            _clock.Time += 1f;
            _haptics.Play(HapticStrength.Tick);

            Assert.AreEqual(1, _device.Played.Count);
        }

        [Test]
        public void Play_None_DoesNothing()
        {
            _haptics.Play(HapticStrength.None);

            Assert.IsEmpty(_device.Played);
        }

        [Test]
        public void Play_AgainWithinTheMinimumGap_IsDropped()
        {
            _haptics.Play(HapticStrength.Tick);
            _clock.Time += _config.MinGap * 0.5f;

            _haptics.Play(HapticStrength.Heavy);

            Assert.AreEqual(1, _device.Played.Count);
        }

        [Test]
        public void Play_AfterTheMinimumGap_VibratesAgain()
        {
            _haptics.Play(HapticStrength.Tick);
            _clock.Time += _config.MinGap * 2f;

            _haptics.Play(HapticStrength.Heavy);

            CollectionAssert.AreEqual(new[] { HapticStrength.Tick, HapticStrength.Heavy }, _device.Played);
        }

        [Test]
        public void Play_ADroppedCall_DoesNotTakeTheGap()
        {
            _settings.Haptics = false;
            _haptics.Play(HapticStrength.Tick);
            _settings.Haptics = true;

            _haptics.Play(HapticStrength.Tick);

            Assert.AreEqual(1, _device.Played.Count);
        }

        [Test]
        public void Pulse_GivesEachStrengthItsOwn()
        {
            Assert.Less(_config.Pulse(HapticStrength.Tick).Milliseconds, _config.Pulse(HapticStrength.Heavy).Milliseconds);
            Assert.Less(_config.Pulse(HapticStrength.Light).Amplitude, _config.Pulse(HapticStrength.Heavy).Amplitude);
        }

        private sealed class RecordingDevice : IHapticDevice
        {
            public List<HapticStrength> Played { get; } = new List<HapticStrength>();

            public void Vibrate(HapticStrength strength) => Played.Add(strength);
        }

        private sealed class FakeGameSettings : IGameSettings
        {
            public bool Haptics { get; set; }

            public void SetHaptics(bool on) => Haptics = on;
        }
    }
}
