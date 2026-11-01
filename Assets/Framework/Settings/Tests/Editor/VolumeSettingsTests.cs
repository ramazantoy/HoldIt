using System.Collections.Generic;
using System.Text.RegularExpressions;
using AF.Audio;
using AF.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Settings.Tests
{
    public class VolumeSettingsTests
    {
        private static readonly AudioChannel _master = new AudioChannel("Master", "MasterVolume");
        private static readonly AudioChannel _music = new AudioChannel("Music", "MusicVolume");

        private AudioOptions _options;
        private FakeVolumes _volumes;
        private VolumeSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _options = new AudioOptions { Music = _music }.Add(_master).Add(_music);
            _volumes = new FakeVolumes(_options);
            _settings = new VolumeSettings(_volumes, _options);
        }

        [Test]
        public void Restore_SetsEveryChannel_MissingOnesToFull()
        {
            _volumes.Set(_master, 0.2f);

            Restore(new VolumeSettingsData { volumes = new Dictionary<string, float> { ["Music"] = 0.5f } });

            Assert.AreEqual(1f, _volumes.Get(_master), "A channel added after the file was written starts at full volume.");
            Assert.AreEqual(0.5f, _volumes.Get(_music));
        }

        [Test]
        public void Restore_DropsUnknownChannels_AndBadValues()
        {
            LogAssert.Expect(LogType.Warning, new Regex("no audio channel for the saved volume\\(s\\) of Voice"));

            Restore(new VolumeSettingsData { volumes = new Dictionary<string, float> { ["Voice"] = 0.3f, ["Music"] = float.NaN, ["Master"] = 4f } });

            Assert.AreEqual(1f, _volumes.Get(_music));
            Assert.AreEqual(1f, _volumes.Get(_master), "Clamped by IAudioVolumes.");
        }

        [Test]
        public void Restore_NullVolumes_IsTheDefault()
        {
            _volumes.Set(_music, 0.1f);

            Restore(new VolumeSettingsData { volumes = null });

            Assert.AreEqual(1f, _volumes.Get(_music));
        }

        [Test]
        public void MuteWhenUnfocused_IsSavedOnlyWhenItDiffersFromTheOptions()
        {
            Assert.IsNull(Capture().muteWhenUnfocused);

            _volumes.MuteWhenUnfocused = false;
            Assert.AreEqual(false, Capture().muteWhenUnfocused);

            Restore(new VolumeSettingsData());
            Assert.IsTrue(_volumes.MuteWhenUnfocused, "Unset follows AudioOptions.MuteWhenUnfocused.");
        }

        [Test]
        public void Capture_ReadsEveryChannelFromIAudioVolumes()
        {
            _volumes.Set(_music, 0.25f);

            var data = Capture();

            CollectionAssert.AreEquivalent(new Dictionary<string, float> { ["Master"] = 1f, ["Music"] = 0.25f }, data.volumes);
        }

        private void Restore(VolumeSettingsData data) => ((ISavable)_settings).RestoreData(data);
        private VolumeSettingsData Capture() => (VolumeSettingsData)((ISavable)_settings).CaptureData();
    }
}
