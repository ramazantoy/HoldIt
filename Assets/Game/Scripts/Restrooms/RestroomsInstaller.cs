using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Restrooms
{
    public static class RestroomsInstaller
    {
        /// <summary>
        /// In the gameplay scene's scope, which outlasts a level: the restrooms' message and their view. The message
        /// lives here, not in a level's scope, so what listens to it outside a level keeps listening.
        /// </summary>
        public static void UseRestroomsFrame(this IContainerBuilder builder, MessagePipeOptions messagePipe, RestroomView view)
        {
            builder.RegisterMessageBroker<StallStateChanged>(messagePipe);
            builder.RegisterComponent(view);
        }

        /// <summary>
        /// In a level's scope that has the board: the restrooms (<see cref="IRestrooms"/>) and their stalls built in the
        /// scene's view. <paramref name="stallCount"/> is the map's: how many stalls each restroom has.
        /// </summary>
        public static void UseRestrooms(this IContainerBuilder builder, RestroomConfig config, StallSet set, int stallCount)
        {
            builder.RegisterInstance(config);
            builder.RegisterInstance(set);
            builder.RegisterEntryPoint<RestroomSystem>().As<IRestrooms>().WithParameter(stallCount);
            builder.RegisterEntryPoint<RestroomBuilder>();
        }
    }
}
