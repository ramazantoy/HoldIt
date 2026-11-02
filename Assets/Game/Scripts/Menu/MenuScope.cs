using AF.Core;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Menu
{
    /// <summary>The menu scene's scope: its two screens and the flow between them.</summary>
    public sealed class MenuScope : SceneScope
    {
        [SerializeField, Required] private MainMenuView _mainMenu;
        [SerializeField, Required] private SettingsView _settings;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterComponent(_mainMenu).As<IMainMenuView>();
            builder.RegisterComponent(_settings).As<ISettingsView>();
            builder.Register<AppExit>(Lifetime.Singleton).As<IAppExit>();
            builder.RegisterEntryPoint<MenuFlow>();
        }
    }
}
