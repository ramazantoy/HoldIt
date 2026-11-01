using System;
using System.Linq;
using AF.Core;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace AF.Platform
{
    public static class PlatformInstaller
    {
        private static readonly Type[] _contracts = { typeof(IAchievements), typeof(IPlatformOverlay) };

        /// <summary>
        /// Registers the platform services in the root scope through <paramref name="backend"/>; null uses
        /// <see cref="MockPlatform"/>, the backend of the Editor and of builds without a store. Throws when the backend leaves
        /// a platform contract unregistered.
        /// </summary>
        public static void UsePlatform(this IContainerBuilder builder, MessagePipeOptions messagePipe, PlatformOptions options = null,
            IPlatformBackend backend = null)
        {
            if (messagePipe == null)
                throw new ArgumentNullException(nameof(messagePipe));
            options ??= new PlatformOptions();
            backend ??= new MockPlatform();

            builder.RegisterMessageBroker<PlatformOverlayChanged>(messagePipe);
            builder.RegisterInstance(options);
            backend.Install(builder, options);

            var missing = _contracts
                .Where(contract => !builder.Exists(contract, includeInterfaceTypes: true))
                .Select(contract => contract.Name)
                .ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException(
                    $"The {backend.Name} platform backend registers no {string.Join(", ", missing)}. Every backend implements " +
                    "every platform contract; register a no-op implementation where the store has no such feature.");

            if (options.PauseOnOverlay)
                builder.RegisterEntryPoint<OverlayPause>();
            builder.RegisterBuildCallback(_ => Log.Info($"Platform: {backend.Name}"));
        }
    }
}
