using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace AF.Core
{
    /// <summary>Leaves the Boot state for the scene's <see cref="BootTarget"/> once the app has booted.</summary>
    internal sealed class SceneEntry : IAsyncStartable
    {
        private readonly BootTarget _target;
        private readonly IAppBoot _boot;
        private readonly IGameManager _game;

        public SceneEntry(BootTarget target, IAppBoot boot, IGameManager game)
        {
            _target = target;
            _boot = boot;
            _game = game;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            // An intro or a similar scene leaves Boot itself.
            if (_target == BootTarget.Manual)
                return;

            try
            {
                await _boot.WaitReady(cancellation);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                return;
            }
            catch (Exception)
            {
                // AppBoot has already logged the failure; the game stays in Boot.
                return;
            }

            if (_game.Current is not BootState || _game.IsTransitioning)
                return;

            switch (_target)
            {
                case BootTarget.Menu:
                    await _game.GoTo<MenuState>();
                    break;
                case BootTarget.Playing:
                    await _game.GoTo<PlayingState>();
                    break;
            }
        }
    }
}
