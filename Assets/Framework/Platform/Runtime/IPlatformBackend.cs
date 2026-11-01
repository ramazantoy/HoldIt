using VContainer;

namespace AF.Platform
{
    /// <summary>
    /// One store or platform: Mock, Steam, later others. <see cref="Install"/> registers an implementation of every platform
    /// contract (<see cref="IAchievements"/>, <see cref="IPlatformOverlay"/>); <c>UsePlatform</c> fails when one is missing,
    /// so a store without a feature registers a no-op implementation. A backend that starts asynchronously registers an
    /// <c>IBootStep</c>.
    /// </summary>
    public interface IPlatformBackend
    {
        /// <summary>For logs and reports.</summary>
        string Name { get; }

        void Install(IContainerBuilder builder, PlatformOptions options);
    }
}
