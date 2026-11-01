using System;
using AF.Core;
using AF.Save;
using MessagePipe;
using VContainer;

namespace AF.Localization
{
    public static class LocalizationInstaller
    {
        /// <summary>
        /// Registers the language in the root scope: <see cref="ILocaleService"/>, saved with the global data, and a boot step
        /// that waits for Unity Localization's preloaded tables. Call it after <c>UseSave</c>, so its boot step runs after the
        /// saved language is restored.
        /// </summary>
        public static void UseLocalization(this IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            if (messagePipe == null)
                throw new ArgumentNullException(nameof(messagePipe));
            if (!builder.Exists(typeof(ISaveRegistry), includeInterfaceTypes: true))
                throw new InvalidOperationException("Call builder.UseSave() before builder.UseLocalization(): the language is saved through it.");

            builder.RegisterMessageBroker<LocaleChanged>(messagePipe);
            builder.Register<LocaleService>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<LocalizationBootStep>(Lifetime.Singleton).As<IBootStep>();
        }
    }
}
