using System;
using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace HoldIt.Intro
{
    /// <summary>
    /// Plays the splash video while the boot steps run, then opens the menu once both are done. If a boot step fails, the game
    /// stays in the intro (AppBoot has logged it).
    /// </summary>
    internal sealed class IntroFlow : IAsyncStartable
    {
        private readonly IIntroVideo _video;
        private readonly IAppBoot _boot;
        private readonly ISceneLoader _scenes;

        public IntroFlow(IIntroVideo video, IAppBoot boot, ISceneLoader scenes)
        {
            _video = video;
            _boot = boot;
            _scenes = scenes;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            try
            {
                await UniTask.WhenAll(_video.Play(cancellation), _boot.WaitReady(cancellation));
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                return;
            }

            await _scenes.Load<MenuState>(Scenes.Menu);
        }
    }
}
