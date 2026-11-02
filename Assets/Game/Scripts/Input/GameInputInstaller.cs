using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Input
{
    public static class GameInputInstaller
    {
        /// <summary>
        /// Registers <see cref="IPointerInput"/> and its messages in a scene scope; the Gameplay map it reads is on only in
        /// the gameplay mode.
        /// </summary>
        public static void UsePointerInput(this IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterMessageBroker<PointerDown>(messagePipe);
            builder.RegisterMessageBroker<PointerMoved>(messagePipe);
            builder.RegisterMessageBroker<PointerUp>(messagePipe);

            builder.Register<EventSystemUiHitTest>(Lifetime.Singleton).As<IUiHitTest>();
            builder.RegisterEntryPoint<PointerInput>().As<IPointerInput>();
        }

        /// <summary>Registers the back button and <see cref="BackPressed"/> in the root scope, after UseInput.</summary>
        public static void UseBackButton(this IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterMessageBroker<BackPressed>(messagePipe);
            builder.RegisterEntryPoint<BackButton>();
        }
    }
}
