using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AF.Core;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;

namespace AF.Audio.Tests
{
    public class AudioVolumesTests
    {
        private static readonly AudioChannel _master = new AudioChannel("Master", "MasterVolume");
        private static readonly AudioChannel _music = new AudioChannel("Music", "MusicVolume");

        private IObjectResolver _container;
        private IPauseService _pause;
        private FakeListener _listener;
        private Dictionary<string, float> _mixer;
        private AudioVolumes _volumes;

        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<PauseChanged>(messagePipe);
            builder.RegisterMessageBroker<AudioVolumeChanged>(messagePipe);
            builder.Register<IPauseService, PauseService>(Lifetime.Singleton);
            _container = builder.Build();
            _pause = _container.Resolve<IPauseService>();
            _listener = new FakeListener();
            _mixer = new Dictionary<string, float>();
            _volumes = Create(followFocus: false);
        }

        [TearDown]
        public void TearDown()
        {
            _volumes.Dispose();
            _container.Dispose();
        }

        [Test]
        public void ListenerPause_FollowsThePauseService()
        {
            var handle = _pause.Request("menu");
            Assert.IsTrue(_listener.Paused);

            handle.Dispose();
            Assert.IsFalse(_listener.Paused);
        }

        [Test]
        public void Volumes_ReachTheMixerFromStart_InDecibels()
        {
            _volumes.Set(_music, 0.5f);
            CollectionAssert.IsEmpty(_mixer, "The mixer may ignore values set before Start.");

            _volumes.Start();
            Assert.AreEqual(0f, _mixer["MasterVolume"], 0.001f);
            Assert.AreEqual(-6.02f, _mixer["MusicVolume"], 0.01f);

            _volumes.Set(_master, 0f);
            Assert.AreEqual(-80f, _mixer["MasterVolume"]);
            _volumes.Set(_master, 3f);
            Assert.AreEqual(1f, _volumes.Get(_master), "Clamped to 0–1.");
        }

        [Test]
        public void Set_PublishesTheNewVolume_OnlyWhenItChanges()
        {
            var changes = new List<AudioVolumeChanged>();
            using var subscription = _container.Resolve<ISubscriber<AudioVolumeChanged>>().Subscribe(changes.Add);

            _volumes.Set(_music, 0.5f);
            _volumes.Set(_music, 0.5f);
            _volumes.Set(_master, 1f);
            _volumes.Set(_master, 7f);

            Assert.AreEqual(1, changes.Count, "Setting the current value, clamped or not, publishes nothing.");
            Assert.AreSame(_music, changes[0].Channel);
            Assert.AreEqual(0.5f, changes[0].Volume);
            Assert.AreEqual(0.5f, _volumes.Get(changes[0].Channel), "Published after the value is stored.");
        }

        [Test]
        public void UnknownChannel_Throws()
        {
            Assert.Throws<ArgumentException>(() => _volumes.Set(new AudioChannel("Voice", "VoiceVolume"), 1f));
        }

        [Test]
        public void Mute_LastsUntilEveryHandleIsDisposed()
        {
            var ad = _volumes.Mute("ad");
            var video = _volumes.Mute("video");
            Assert.AreEqual(0f, _listener.Volume);

            ad.Dispose();
            ad.Dispose();
            Assert.AreEqual(0f, _listener.Volume);

            video.Dispose();
            Assert.AreEqual(1f, _listener.Volume);
        }

        [Test]
        public void LosingFocus_Mutes_WhileTheOptionIsOn()
        {
            _volumes.Dispose();
            _volumes = Create(followFocus: true);
            _volumes.OnFocusChanged(true);

            _volumes.OnFocusChanged(false);
            Assert.AreEqual(0f, _listener.Volume);

            _volumes.MuteWhenUnfocused = false;
            Assert.AreEqual(1f, _listener.Volume);

            _volumes.MuteWhenUnfocused = true;
            Assert.AreEqual(0f, _listener.Volume);

            _volumes.OnFocusChanged(true);
            Assert.AreEqual(1f, _listener.Volume);
        }

        [Test]
        public void Focus_IsIgnored_WhenNotFollowed()
        {
            _volumes.OnFocusChanged(false);

            Assert.AreEqual(1f, _listener.Volume);
        }

        [Test]
        public void Focus_IsFollowedInDesktopPlayersOnly()
        {
            Assert.IsTrue(AudioVolumes.FollowsFocus(isEditor: false, isMobile: false));
            Assert.IsFalse(AudioVolumes.FollowsFocus(isEditor: true, isMobile: false));
            Assert.IsFalse(AudioVolumes.FollowsFocus(isEditor: false, isMobile: true));
        }

        [Test]
        public void ListenerChangedElsewhere_IsReported()
        {
            _listener.ChangeOutside();

            LogAssert.Expect(LogType.Error, new Regex("AudioListener.pause was changed outside the Audio module"));
            _pause.Request("menu");
        }

        [Test]
        public void ListenerVolumeChangedElsewhere_IsReported()
        {
            _listener.ChangeVolumeOutside();

            LogAssert.Expect(LogType.Error, new Regex("AudioListener.volume was changed outside the Audio module"));
            _volumes.Mute("ad");
        }

        [Test]
        public void Dispose_ResetsTheListener()
        {
            _pause.Request("menu");
            _volumes.Mute("ad");

            _volumes.Dispose();

            Assert.IsFalse(_listener.Paused);
            Assert.AreEqual(1f, _listener.Volume);
        }

        private AudioVolumes Create(bool followFocus)
        {
            var options = new AudioOptions { Music = _music }.Add(_master).Add(_music);
            var setup = new AudioSetup(options, null, (parameter, value) => _mixer[parameter] = value);
            return new AudioVolumes(setup, _pause, _container.Resolve<ISubscriber<PauseChanged>>(),
                _container.Resolve<IPublisher<AudioVolumeChanged>>(), _listener, followFocus);
        }

        private sealed class FakeListener : AudioListenerAccess
        {
            private bool _outside;
            private bool _outsideVolume;

            public bool Paused { get; private set; }
            public float Volume { get; private set; } = 1f;

            public void ChangeOutside() => _outside = true;

            public void ChangeVolumeOutside() => _outsideVolume = true;

            public override bool GetPause() => _outside ? !Paused : Paused;
            public override float GetVolume() => _outsideVolume ? Volume * 0.5f : Volume;
            protected override void WritePause(bool paused) => Paused = paused;
            protected override void WriteVolume(float volume) => Volume = volume;
        }
    }
}
