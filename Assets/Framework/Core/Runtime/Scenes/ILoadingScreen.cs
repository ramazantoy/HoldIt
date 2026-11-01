using System.Threading;
using Cysharp.Threading.Tasks;

namespace AF.Core
{
    /// <summary>
    /// The game's loading screen, shown by <see cref="ISceneLoader"/> while it replaces the scenes. Lives in the root scope
    /// (a component on the root prefab, registered with <c>builder.RegisterComponent(field).As&lt;ILoadingScreen&gt;()</c>), so a
    /// scene change does not destroy it. At most one may be registered; without one, scenes change without a screen.
    /// </summary>
    /// <remarks>
    /// The game may be paused while it loads: animate with unscaled time, and await tweens with
    /// <c>ToUniTask(cancellation)</c>. The token is cancelled when the app quits, Play stops or the root scope is disposed.
    /// </remarks>
    public interface ILoadingScreen
    {
        /// <summary>Covers the screen. Returns when the old scene can no longer be seen.</summary>
        UniTask Show(CancellationToken cancellation);

        /// <summary>Loading progress of the new scene, from 0 to 1. Called every frame while it loads.</summary>
        void SetProgress(float progress);

        /// <summary>Uncovers the new scene. Input comes back once it returns.</summary>
        UniTask Hide(CancellationToken cancellation);
    }
}
