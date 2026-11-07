using VContainer;

namespace HoldIt.Pooling
{
    public static class PoolingInstaller
    {
        /// <summary>
        /// In the root scope: the pools that outlast the scenes and the levels, and the boot step that warms them while the
        /// intro plays. After <c>UseFeedback</c> and the like, so the step runs once what the views need is ready.
        /// </summary>
        public static void UsePersistentPools(this IContainerBuilder builder, PoolWarmup warmup)
        {
            builder.RegisterInstance(warmup);
            builder.Register<PersistentPools>(Lifetime.Singleton).As<IPersistentPools>();
            builder.Register<PoolWarmupStep>(Lifetime.Singleton).As<AF.Core.IBootStep>();
        }

        /// <summary>In a level's scope: its loan of the root's pools, which it gives back with the level.</summary>
        public static void UseLevelPools(this IContainerBuilder builder)
        {
            builder.Register<LevelPools>(Lifetime.Singleton).As<ILevelPools>();
        }
    }
}
