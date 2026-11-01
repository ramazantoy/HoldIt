using System;
using System.Linq;
using AF.Core;
using MessagePipe;
using UnityEngine.Audio;
using VContainer;
using VContainer.Unity;

namespace AF.Audio
{
    public static class AudioInstaller
    {
        /// <summary>
        /// Registers the audio services in the root scope: sound cues through <see cref="ISounds"/> (one per scope) and
        /// <see cref="IGlobalSounds"/>, music, channel volumes and muting, and the game's single AudioListener. Throws when a
        /// channel's mixer group or volume parameter is missing from <paramref name="mixer"/>.
        /// </summary>
        public static void UseAudio(this IContainerBuilder builder, AudioMixer mixer, MessagePipeOptions messagePipe, AudioOptions options)
        {
            if (mixer == null)
                throw new ArgumentNullException(nameof(mixer), "Assign the audio mixer on the root scope.");
            if (messagePipe == null)
                throw new ArgumentNullException(nameof(messagePipe));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            Install(builder, messagePipe, AudioSetup.FromMixer(mixer, options));
            builder.RegisterBuildCallback(_ => Log.Info(
                $"Audio: mixer {mixer.name}, channels {string.Join(", ", options.Channels.Select(channel => channel.Group))}, " +
                $"{options.PrewarmVoices} voices"));
        }

        // Tests install with their own setup: a mixer cannot be made without an asset.
        internal static void Install(IContainerBuilder builder, MessagePipeOptions messagePipe, AudioSetup setup)
        {
            builder.RegisterMessageBroker<AudioVolumeChanged>(messagePipe);
            builder.RegisterInstance(setup.Options);
            builder.RegisterInstance(setup);
            builder.Register<AudioRig>(Lifetime.Singleton);
            builder.RegisterEntryPoint<VoicePlayer>().AsSelf();
            // Scoped: each scope's instance owns the sounds played through it.
            builder.Register<ISounds, ScopeSounds>(Lifetime.Scoped);
            builder.Register<IGlobalSounds, ScopeSounds>(Lifetime.Singleton);
            builder.RegisterEntryPoint<MusicPlayer>().As<IMusic>();
            builder.RegisterEntryPoint<AudioVolumes>().As<IAudioVolumes>();
            builder.RegisterEntryPoint<ListenerFollower>().As<IAudioListener>();
        }
    }
}
