using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AF.Audio.Tests
{
    public class MusicTests
    {
        private AudioRig _rig;
        private MusicPlayer _music;
        private AudioClip _first;
        private AudioClip _second;

        [SetUp]
        public void SetUp()
        {
            _rig = new AudioRig((Transform)null);
            _music = new MusicPlayer(_rig, new AudioSetup(new AudioOptions(), null, (parameter, value) => { }));
            _first = AudioClip.Create("First", 44100, 1, 44100, false);
            _second = AudioClip.Create("Second", 44100, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            _music.Dispose();
            _rig.Dispose();
            Object.DestroyImmediate(_first);
            Object.DestroyImmediate(_second);
        }

        [Test]
        public void Sources_LoopAndPlayThroughPause()
        {
            foreach (var source in Sources())
            {
                Assert.IsTrue(source.loop);
                Assert.IsTrue(source.ignoreListenerPause);
                Assert.IsFalse(source.playOnAwake);
            }
        }

        [Test]
        public void PlayWithoutFade_SwitchesAtOnce()
        {
            _music.Play(_first, 0f);
            Assert.AreSame(_first, _music.Current);
            Assert.AreEqual(1f, SourceOf(_first).volume);

            _music.Play(_second, 0f);
            Assert.AreSame(_second, _music.Current);
            Assert.IsNull(SourceOf(_first), "The old track is silenced.");
            Assert.AreEqual(1f, SourceOf(_second).volume);

            _music.Stop(0f);
            Assert.IsNull(_music.Current);
            Assert.IsTrue(Sources().All(source => source.clip == null));
        }

        [Test]
        public void PlayingTheCurrentTrack_DoesNothing()
        {
            _music.Play(_first, 0f);
            var source = SourceOf(_first);

            _music.Play(_first);

            Assert.AreSame(source, SourceOf(_first));
            Assert.AreEqual(1f, source.volume);
        }

        [UnityTest]
        public IEnumerator Crossfade_EndsWithTheNewTrackOnly()
        {
            _music.Play(_first, 0f);
            _music.Play(_second, 0.1f);
            var incoming = SourceOf(_second);
            Assert.Less(incoming.volume, 1f, "Fading in.");

            var deadline = EditorApplication.timeSinceStartup + 2.0;
            while (incoming.volume < 1f && EditorApplication.timeSinceStartup < deadline)
                yield return null;
            // The outgoing fade ends on the same update.
            yield return null;

            Assert.AreEqual(1f, incoming.volume, 0.001f);
            var outgoing = SourceOf(_first);
            Assert.IsTrue(outgoing == null || outgoing.volume <= 0.001f);
        }

        [Test]
        public void StoppedByAnAudioReset_TheCurrentTrackPlaysAgain()
        {
            _music.Play(_first, 0f);
            var source = SourceOf(_first);
            source.Stop();

            _music.OnAudioConfigurationChanged(true);
            _music.Tick();

            Assert.IsTrue(source.isPlaying);
        }

        private AudioSource[] Sources() => _rig.Root.GetComponentsInChildren<AudioSource>();

        private AudioSource SourceOf(AudioClip clip) => Sources().SingleOrDefault(source => source.clip == clip);
    }
}
