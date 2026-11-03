using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Characters
{
    public static class CharactersInstaller
    {
        /// <summary>
        /// Registers the characters (<see cref="ICharacters"/>), their messages and their views in a scene scope that already
        /// has the board.
        /// </summary>
        public static void UseCharacters(this IContainerBuilder builder, MessagePipeOptions messagePipe,
            CharacterConfig config, CharacterSet set)
        {
            builder.RegisterMessageBroker<CharacterSpawned>(messagePipe);
            builder.RegisterMessageBroker<CharacterStateChanged>(messagePipe);

            builder.RegisterInstance(config);
            builder.RegisterInstance(set);
            builder.RegisterEntryPoint<CharacterSystem>().As<ICharacters>();
            builder.RegisterEntryPoint<CharacterViews>();
        }
    }
}
