using System;
using AF.Audio;
using AF.Input;
using AF.Save;
using MessagePipe;
using VContainer;

namespace AF.Settings
{
    public static class SettingsInstaller
    {
        /// <summary>
        /// Registers the player's settings in the root scope: display settings (<see cref="IDisplaySettings"/>) and the saving
        /// of channel volumes, both with this device's save data. They are applied while the save data loads during boot.
        /// Call it after <c>UseSave</c>, <c>UseAudio</c> and <c>UseInput</c>.
        /// </summary>
        public static void UseSettings(this IContainerBuilder builder, MessagePipeOptions messagePipe, SettingsOptions options = null)
        {
            if (messagePipe == null)
                throw new ArgumentNullException(nameof(messagePipe));
            Require<ISaveRegistry>(builder, "UseSave", "settings are saved through it");
            Require<IAudioVolumes>(builder, "UseAudio", "channel volumes are saved from it");
            Require<IInputModeService>(builder, "UseInput", "cursor confinement goes through it");
            options ??= new SettingsOptions();
            // A default the setters would refuse would otherwise apply as it is, with a warning at every startup.
            CheckLimit(options.FrameRateLimit, nameof(SettingsOptions.FrameRateLimit));
            CheckLimit(options.MobileFrameRateLimit, nameof(SettingsOptions.MobileFrameRateLimit));

            builder.RegisterMessageBroker<DisplaySettingsChanged>(messagePipe);
            builder.RegisterInstance(options);
            builder.Register<DisplaySettings>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<VolumeSettings>(Lifetime.Singleton).AsImplementedInterfaces();
        }

        private static void CheckLimit(int framesPerSecond, string option)
        {
            if (!DisplaySettings.IsValidLimit(framesPerSecond))
                throw new ArgumentException($"SettingsOptions.{option} is {framesPerSecond}: use 0 or at least {DisplaySettings.MinFrameRateLimit}.", "options");
        }

        private static void Require<T>(IContainerBuilder builder, string installer, string reason)
        {
            if (!builder.Exists(typeof(T), includeInterfaceTypes: true))
                throw new InvalidOperationException($"Call builder.{installer}() before builder.UseSettings(): {reason}.");
        }
    }
}
