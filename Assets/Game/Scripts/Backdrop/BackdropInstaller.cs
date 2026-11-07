using VContainer;
using VContainer.Unity;

namespace HoldIt.Backdrop
{
    public static class BackdropInstaller
    {
        /// <summary>
        /// In the root scope: remembers which palette the backdrop is on, so it outlasts the gameplay scene, which is rebuilt
        /// for every level.
        /// </summary>
        public static void UseBackdropMemory(this IContainerBuilder builder) =>
            builder.Register<IBackdropMemory>(_ => new BackdropMemory(new System.Random()), Lifetime.Singleton);

        /// <summary>In a gameplay scene scope: paints the backdrop when the level opens, from the palette the last level left.</summary>
        public static void UseBackdrop(this IContainerBuilder builder, BackdropConfig config)
        {
            builder.RegisterInstance(config);
            builder.RegisterEntryPoint<BackdropPlayer>();
        }
    }
}
