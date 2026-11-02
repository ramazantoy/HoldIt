using AF.Core;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Intro
{
    /// <summary>
    /// The intro scene's scope; the intro is the first scene of the build. Its Boot Target is Manual: the intro leaves Boot
    /// itself by loading the menu. Play started from another scene in the Editor skips it.
    /// </summary>
    public sealed class IntroScope : SceneScope
    {
        [SerializeField, Required] private IntroVideoView _video;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.RegisterComponent(_video).As<IIntroVideo>();
            builder.RegisterEntryPoint<IntroFlow>();
        }
    }
}
