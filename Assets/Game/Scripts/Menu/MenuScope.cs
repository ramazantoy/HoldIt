using AF.Core;
using HoldIt.Feedback;
using HoldIt.Levels;
using HoldIt.Settings;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Menu
{
    /// <summary>The menu scene's scope: its screens (main menu, level select, settings) and the flow between them.</summary>
    public sealed class MenuScope : SceneScope
    {
        [SerializeField, Required] private MainMenuView _mainMenu;
        [SerializeField, Required] private LevelSelectView _levelSelect;
        [SerializeField, Required] private SettingsView _settings;

        [SerializeField, Required, Tooltip("The lives: the most stars a level can be won with.")]
        private RunConfig _runConfig;

        [SerializeField, Tooltip("The menu's music; empty for none.")]
        private AudioClip _music;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterComponent(_mainMenu).As<IMainMenuView>();
            builder.RegisterComponent(_levelSelect).As<ILevelSelectView>();
            builder.RegisterInstance(_runConfig);
            builder.RegisterEntryPoint<LevelSelect>().As<ILevelSelect>();
            builder.UseSettingsScreen(_settings);
            builder.Register<AppExit>(Lifetime.Singleton).As<IAppExit>();
            builder.RegisterEntryPoint<MenuFlow>();
            builder.UseSceneMusic(_music);
        }
    }
}
