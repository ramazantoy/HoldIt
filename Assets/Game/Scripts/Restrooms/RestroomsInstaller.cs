using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Restrooms
{
    public static class RestroomsInstaller
    {
        /// <summary>
        /// Registers the restrooms (<see cref="IRestrooms"/>), their message and their view in a scene scope that already
        /// has the board. <paramref name="stallCount"/> is the map's: how many stalls each restroom has.
        /// </summary>
        public static void UseRestrooms(this IContainerBuilder builder, MessagePipeOptions messagePipe,
            RestroomConfig config, StallSet set, int stallCount, RestroomView view)
        {
            builder.RegisterMessageBroker<StallStateChanged>(messagePipe);

            builder.RegisterInstance(config);
            builder.RegisterInstance(set);
            builder.RegisterEntryPoint<RestroomSystem>().As<IRestrooms>().WithParameter(stallCount);
            builder.RegisterComponent(view);
        }
    }
}
