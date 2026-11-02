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
        [SerializeField, Min(0f), Tooltip("The intro shows at least this long; the menu opens once the boot steps are done too.")]
        private float _minimumSeconds = 2f;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe) =>
            builder.RegisterEntryPoint<IntroFlow>().WithParameter(_minimumSeconds);
    }
}
