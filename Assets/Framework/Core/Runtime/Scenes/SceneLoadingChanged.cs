namespace AF.Core
{
    /// <summary>
    /// Published when <see cref="ISceneLoader"/> starts and finishes replacing the scenes. Additive loads do not publish it.
    /// </summary>
    public readonly struct SceneLoadingChanged
    {
        public readonly bool IsLoading;

        /// <summary>The scene being loaded.</summary>
        public readonly string Scene;

        public SceneLoadingChanged(bool isLoading, string scene)
        {
            IsLoading = isLoading;
            Scene = scene;
        }
    }
}
