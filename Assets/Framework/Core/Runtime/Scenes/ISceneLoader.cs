using Cysharp.Threading.Tasks;

namespace AF.Core
{
    /// <summary>
    /// Changes scenes. Scenes are named as in the build's scene list. One operation runs at a time; another call while one is
    /// running throws <see cref="System.InvalidOperationException"/>, and a scene that is not in the build throws
    /// <see cref="System.ArgumentException"/> before anything starts.
    /// </summary>
    /// <remarks>
    /// The methods take no cancellation token: a scene change cancelled halfway leaves no consistent state, and the token of
    /// the object that starts it (a button in the old scene) is cancelled by the change itself. A caller in the scene being
    /// replaced is destroyed while it awaits; it must not touch itself after the await.
    /// </remarks>
    public interface ISceneLoader
    {
        /// <summary>The main scene: the one last loaded with <see cref="Load"/>, or the one Play started in.</summary>
        string Current { get; }

        /// <summary>True while <see cref="Load"/> replaces the scenes; input is blocked meanwhile. Additive loads do not set it.</summary>
        bool IsLoading { get; }

        /// <summary>
        /// Replaces every loaded scene with <paramref name="scene"/> and stays in the current game state: the next level, or the
        /// same one again. A reloaded scene's savables come back with the state captured when it unloaded; to return to the
        /// last save, call <c>LoadSlot</c> first.
        /// </summary>
        UniTask Load(string scene);

        /// <summary>
        /// Like <see cref="Load"/>, and enters <typeparamref name="TState"/> while the loading screen covers the old scene,
        /// before it unloads: the old state exits while its scene still exists, and the new scene starts in the new state.
        /// </summary>
        UniTask Load<TState>(string scene) where TState : IGameState;

        /// <summary>
        /// Adds <paramref name="scene"/> to the main scene, without the loading screen or an input block. Its scope is a child
        /// of the main scene's scope, so it can inject the main scene's services. <see cref="Load"/> unloads it with the rest.
        /// </summary>
        UniTask LoadAdditive(string scene);

        /// <summary>Unloads a scene added with <see cref="LoadAdditive"/>.</summary>
        UniTask UnloadAdditive(string scene);
    }
}
