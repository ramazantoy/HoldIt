using UnityEngine;
using VContainer;

namespace HoldIt.Haptics
{
    public static class HapticsInstaller
    {
        /// <summary>
        /// Registers <see cref="IHaptics"/> in the root scope, after the game settings it reads. On an Android device it
        /// vibrates the phone; anywhere else (the Editor, desktop) it only logs.
        /// </summary>
        public static void UseHaptics(this IContainerBuilder builder, HapticsConfig config)
        {
            builder.RegisterInstance(config);
            builder.Register<IHapticDevice>(_ => Application.platform == RuntimePlatform.Android
                ? new AndroidHapticDevice(config)
                : new LogHapticDevice(), Lifetime.Singleton);
            builder.Register<HapticPlayer>(Lifetime.Singleton).As<IHaptics>();
        }
    }
}
