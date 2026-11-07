using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Characters
{
    public static class CharactersInstaller
    {
        /// <summary>
        /// In the gameplay scene's scope, which outlasts a level: the characters' messages. They live here, not in a
        /// level's scope, so what listens to them outside a level keeps listening.
        /// </summary>
        public static void UseCharacterMessages(this IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterMessageBroker<CharacterSpawned>(messagePipe);
            builder.RegisterMessageBroker<CharacterStateChanged>(messagePipe);
            builder.RegisterMessageBroker<StepRefused>(messagePipe);
            builder.RegisterMessageBroker<CharacterRemoved>(messagePipe);
        }

        /// <summary>
        /// In a level's scope that has the board, the restrooms and the level's pools: the characters
        /// (<see cref="ICharacters"/>) and their views. The views come from the root's pools and go back to them with the
        /// level; the <see cref="CharacterSet"/> they are made with is the root's.
        /// </summary>
        public static void UseCharacters(this IContainerBuilder builder, CharacterConfig config)
        {
            builder.RegisterInstance(config);
            builder.Register<LevelViewContext>(Lifetime.Singleton);
            builder.RegisterEntryPoint<CharacterSystem>().As<ICharacters>();
            builder.RegisterEntryPoint<CharacterViews>();
        }
    }
}
