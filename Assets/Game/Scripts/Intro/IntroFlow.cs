using System;
using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Game
{
    /// <summary>
    /// Placeholder intro: waits for its minimum length and for the boot steps, which run meanwhile, then opens the menu. A real
    /// intro plays its logos, video or timeline instead of the wait.
    /// </summary>
    internal sealed class IntroFlow : IAsyncStartable
    {
        private readonly float _minimumSeconds;
        private readonly IAppBoot _boot;
        private readonly ISceneLoader _scenes;

        public IntroFlow(float minimumSeconds, IAppBoot boot, ISceneLoader scenes)
        {
            _minimumSeconds = minimumSeconds;
            _boot = boot;
            _scenes = scenes;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            try
            {
                await UniTask.WhenAll(
                    UniTask.Delay(TimeSpan.FromSeconds(_minimumSeconds), DelayType.UnscaledDeltaTime, cancellationToken: cancellation),
                    _boot.WaitReady(cancellation));
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                // AppBoot has already logged the failed step; the game stays in the intro.
                return;
            }

            await _scenes.Load<MenuState>(Scenes.Menu);
        }
    }
}
