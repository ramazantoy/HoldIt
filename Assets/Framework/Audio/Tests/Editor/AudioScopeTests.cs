using AF.Core;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace AF.Audio.Tests
{
    // The module's real registrations, with a child scope standing in for a scene.
    public class AudioScopeTests
    {
        private AudioClip _clip;
        private SoundCue _loop;

        [SetUp]
        public void SetUp()
        {
            _clip = AudioClip.Create("Tone", 4410, 1, 44100, false);
            _loop = SoundCue.Create(null, loop: true, clips: _clip);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_loop);
            Object.DestroyImmediate(_clip);
        }

        [Test]
        public void ChildScopeSounds_StopWithTheChild_GlobalOnesDoNot()
        {
            var root = BuildRoot();
            try
            {
                var child = root.CreateScope(_ => { });
                var sceneSounds = child.Resolve<ISounds>();
                Assert.AreNotSame(root.Resolve<ISounds>(), sceneSounds);
                Assert.AreSame(root.Resolve<IGlobalSounds>(), child.Resolve<IGlobalSounds>());

                var scene = sceneSounds.Play(_loop);
                var global = child.Resolve<IGlobalSounds>().Play(_loop);
                var rootOwned = root.Resolve<ISounds>().Play(_loop);

                child.Dispose();

                Assert.IsFalse(scene.IsPlaying);
                Assert.IsTrue(global.IsPlaying);
                Assert.IsTrue(rootOwned.IsPlaying);
            }
            finally
            {
                Assert.DoesNotThrow(() => root.Dispose(), "The root's pools go before the players that used them.");
            }
        }

        private static IObjectResolver BuildRoot()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<PauseChanged>(messagePipe);
            builder.Register<IPauseService, PauseService>(Lifetime.Singleton);
            builder.Register<IPools, Pools>(Lifetime.Scoped);
            var music = new AudioChannel("Music", "MusicVolume");
            var options = new AudioOptions { Music = music, PrewarmVoices = 1 }.Add(music);
            AudioInstaller.Install(builder, messagePipe, new AudioSetup(options, null, (parameter, value) => { }));
            return builder.Build();
        }
    }
}
