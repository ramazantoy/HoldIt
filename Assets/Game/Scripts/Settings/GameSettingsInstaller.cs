using VContainer;
using VContainer.Unity;

namespace HoldIt.Settings
{
    public static class GameSettingsInstaller
    {
        /// <summary>Registers the game's own settings in the root scope, as a global savable.</summary>
        public static void UseGameSettings(this IContainerBuilder builder) =>
            builder.Register<GameSettings>(Lifetime.Singleton).AsImplementedInterfaces();

        /// <summary>Registers a scene's settings screen and what drives it (<see cref="ISettingsScreen"/>).</summary>
        public static void UseSettingsScreen(this IContainerBuilder builder, SettingsView view)
        {
            builder.RegisterComponent(view).As<ISettingsView>();
            builder.RegisterEntryPoint<SettingsPresenter>().As<ISettingsScreen>();
        }
    }
}
