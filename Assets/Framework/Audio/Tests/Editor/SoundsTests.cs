using System;
using System.Collections;
using System.Text.RegularExpressions;
using AF.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using Object = UnityEngine.Object;

namespace AF.Audio.Tests
{
    public class SoundsTests
    {
        private IObjectResolver _container;
        private Pools _pools;
        private AudioRig _rig;
        private VoicePlayer _player;
        private bool _listenerPaused;
        private AudioClip _clip;
        private SoundCue _cue;
        private SoundCue _loop;
        private SoundCue _interface;

        [SetUp]
        public void SetUp()
        {
            _container = new ContainerBuilder().Build();
            _pools = new Pools(_container);
            _rig = new AudioRig((Transform)null);
            _player = new VoicePlayer(_pools, _rig, new AudioOptions { PrewarmVoices = 2 }, () => _listenerPaused);
            _clip = AudioClip.Create("Tone", 4410, 1, 44100, false);
            _cue = SoundCue.Create(null, clips: _clip);
            _loop = SoundCue.Create(null, loop: true, clips: _clip);
            _interface = SoundCue.Create(null, playWhilePaused: true, clips: _clip);
        }

        [TearDown]
        public void TearDown()
        {
            _player.Dispose();
            _pools.Dispose();
            _rig.Dispose();
            _container.Dispose();
            Object.DestroyImmediate(_cue);
            Object.DestroyImmediate(_loop);
            Object.DestroyImmediate(_interface);
            Object.DestroyImmediate(_clip);
        }

        [Test]
        public void Voices_ArePrewarmed_FromATemplateUnderTheRig()
        {
            Assert.IsNotNull(_rig.Root.Find("Voice"));
            Assert.AreEqual(2, Object.FindObjectsByType<SoundVoice>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length - 1);
        }

        [Test]
        public void Play_SetsUpTheWholeSource()
        {
            var sounds = new ScopeSounds(_player);

            var flat = sounds.Play(_loop);
            var placed = sounds.PlayAt(_interface, new Vector3(0f, 2f, 0f));

            Assert.IsTrue(flat.IsPlaying);
            Assert.AreEqual(2, _player.Count);
            var source = flat.Voice.Source;
            Assert.AreSame(_clip, source.clip);
            Assert.IsTrue(source.loop);
            Assert.IsFalse(source.ignoreListenerPause);
            Assert.AreEqual(0f, source.spatialBlend);
            source = placed.Voice.Source;
            Assert.IsFalse(source.loop, "Nothing carries over from another play.");
            Assert.IsTrue(source.ignoreListenerPause);
            Assert.AreEqual(1f, source.spatialBlend);
            Assert.AreEqual(new Vector3(0f, 2f, 0f), source.transform.position);
        }

        [Test]
        public void EndingScope_StopsItsSoundsOnly()
        {
            var scene = new ScopeSounds(_player);
            var root = new ScopeSounds(_player);
            var first = scene.Play(_loop);
            var second = scene.PlayAt(_loop, Vector3.zero);
            var kept = root.Play(_loop);

            scene.Dispose();

            Assert.IsFalse(first.IsPlaying);
            Assert.IsFalse(second.IsPlaying);
            Assert.IsTrue(kept.IsPlaying);
            Assert.AreEqual(1, _player.Count);
            Assert.Throws<ObjectDisposedException>(() => scene.Play(_cue));
        }

        [Test]
        public void StaleHandle_DoesNotStopTheNextSound()
        {
            var sounds = new ScopeSounds(_player);
            var first = sounds.Play(_loop);
            first.Stop();
            Assert.IsFalse(first.IsPlaying);

            var second = sounds.Play(_loop);
            Assert.AreSame(first.Voice, second.Voice, "The source is reused.");

            first.Stop();
            Assert.IsTrue(second.IsPlaying);
            Assert.DoesNotThrow(() => default(SoundHandle).Stop());
        }

        [Test]
        public void FinishedSound_GoesBack_UnlessPausedWithTheGame()
        {
            var sounds = new ScopeSounds(_player);
            var game = sounds.Play(_cue);
            var menu = sounds.Play(_interface);
            game.Voice.Source.Stop();
            menu.Voice.Source.Stop();

            _listenerPaused = true;
            _player.Tick();
            Assert.IsTrue(game.IsPlaying, "Paused with the game, not finished.");
            Assert.IsFalse(menu.IsPlaying, "Plays while paused, so it has finished.");

            _listenerPaused = false;
            _player.Tick();
            Assert.IsFalse(game.IsPlaying);
            Assert.AreEqual(0, _player.Count);
        }

        [Test]
        public void CueWithoutClips_IsReportedOnce()
        {
            var empty = SoundCue.Create(null);
            try
            {
                var sounds = new ScopeSounds(_player);
                LogAssert.Expect(LogType.Error, new Regex("has no clip to play"));

                Assert.IsFalse(sounds.Play(empty).IsPlaying);
                Assert.IsFalse(sounds.Play(empty).IsPlaying);
            }
            finally
            {
                Object.DestroyImmediate(empty);
            }
        }

        [UnityTest]
        public IEnumerator StopWithFade_EndsTheSoundWhenTheFadeEnds()
        {
            var handle = new ScopeSounds(_player).Play(_loop);
            var source = handle.Voice.Source;

            handle.Stop(0.1f);
            Assert.IsTrue(handle.IsPlaying, "Still fading out.");

            var deadline = EditorApplication.timeSinceStartup + 2.0;
            while (handle.IsPlaying && EditorApplication.timeSinceStartup < deadline)
            {
                yield return null;
                _player.Tick();
            }

            Assert.IsFalse(handle.IsPlaying);
            Assert.AreEqual(0f, source.volume, 0.001f);
        }

        [Test]
        public void KilledFade_StillEndsTheSound()
        {
            var handle = new ScopeSounds(_player).Play(_loop);
            handle.Stop(5f);

            handle.Voice.Fade.Stop();
            _player.Tick();

            Assert.IsFalse(handle.IsPlaying, "The fade's completion is not the only way back to the pool.");
        }

        [Test]
        public void PlayingVoices_SitUnderTheRig()
        {
            var handle = new ScopeSounds(_player).Play(_cue);

            Assert.AreEqual(_rig.Root, handle.Voice.transform.parent);
        }
    }
}
