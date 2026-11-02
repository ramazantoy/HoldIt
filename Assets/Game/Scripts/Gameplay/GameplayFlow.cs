using System;
using AF.Core;
using Cysharp.Threading.Tasks;
using HoldIt.Input;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// Leaves the game for the menu: the HUD's menu button, or back. Once there is a pause menu (M6), back opens it instead and
    /// closes it while it is open.
    /// </summary>
    internal sealed class GameplayFlow : IInitializable, IDisposable
    {
        private readonly IGameplayHudView _hud;
        private readonly ISceneLoader _scenes;
        private readonly ISubscriber<BackPressed> _back;

        private IDisposable _backSubscription;
        private bool _leaving;

        public GameplayFlow(IGameplayHudView hud, ISceneLoader scenes, ISubscriber<BackPressed> back)
        {
            _hud = hud;
            _scenes = scenes;
            _back = back;
        }

        public void Initialize()
        {
            _hud.MenuClicked += ToMenu;
            _backSubscription = _back.Subscribe(_ => ToMenu());
        }

        public void Dispose()
        {
            _hud.MenuClicked -= ToMenu;
            _backSubscription?.Dispose();
        }

        private void ToMenu()
        {
            if (_leaving)
                return;
            _leaving = true;
            _scenes.Load<MenuState>(Scenes.Menu).Forget();
        }
    }
}
