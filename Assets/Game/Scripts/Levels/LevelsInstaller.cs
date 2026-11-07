using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Levels
{
    public static class LevelsInstaller
    {
        /// <summary>
        /// Registers the level list, the saved progress through it and the cursor over it in the root scope, so they outlast
        /// the scenes.
        /// </summary>
        public static void UseLevels(this IContainerBuilder builder, LevelList levels)
        {
            builder.RegisterInstance(levels);
            builder.Register<LevelProgress>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<LevelCursor>(Lifetime.Singleton).As<ILevelCursor>();
        }

        /// <summary>
        /// In the gameplay scene's scope, which outlasts a level: the run's messages. They live here, not in a level's
        /// scope, so what listens to them outside a level keeps listening.
        /// </summary>
        public static void UseLevelRunMessages(this IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterMessageBroker<ServedChanged>(messagePipe);
            builder.RegisterMessageBroker<LivesChanged>(messagePipe);
            builder.RegisterMessageBroker<LevelEnded>(messagePipe);
        }

        /// <summary>
        /// In a level's scope: the level, the run that keeps its score, and the keeping of a win in the progress.
        /// </summary>
        public static void UseLevelRun(this IContainerBuilder builder, RunConfig config, LevelDefinition level)
        {
            builder.RegisterInstance(config);
            builder.RegisterInstance(level);
            builder.RegisterEntryPoint<LevelRun>().As<ILevelRun>();
            builder.RegisterEntryPoint<ProgressRecorder>();
        }
    }
}
