using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Levels
{
    public static class LevelsInstaller
    {
        /// <summary>Registers the level list and the cursor over it in the root scope, so they outlast the scenes.</summary>
        public static void UseLevels(this IContainerBuilder builder, LevelList levels)
        {
            builder.RegisterInstance(levels);
            builder.Register<LevelCursor>(Lifetime.Singleton).As<ILevelCursor>();
        }

        /// <summary>Registers the level a gameplay scene plays, the run that keeps its score, and the run's messages.</summary>
        public static void UseLevelRun(this IContainerBuilder builder, MessagePipeOptions messagePipe, RunConfig config,
            LevelDefinition level)
        {
            builder.RegisterMessageBroker<ServedChanged>(messagePipe);
            builder.RegisterMessageBroker<LivesChanged>(messagePipe);
            builder.RegisterMessageBroker<LevelEnded>(messagePipe);

            builder.RegisterInstance(config);
            builder.RegisterInstance(level);
            builder.RegisterEntryPoint<LevelRun>().As<ILevelRun>();
        }
    }
}
