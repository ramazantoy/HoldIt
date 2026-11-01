using VContainer;

namespace AF.Platform
{
    /// <summary>
    /// The backend of the Editor and of builds without a store. Nothing reaches a store and nothing is persisted; the mocks
    /// are also registered as themselves, so tests and Play Mode tools can drive them.
    /// </summary>
    public sealed class MockPlatform : IPlatformBackend
    {
        public string Name => "Mock";

        public void Install(IContainerBuilder builder, PlatformOptions options)
        {
            builder.Register<MockAchievements>(Lifetime.Singleton).As<IAchievements>().AsSelf();
            builder.Register<MockPlatformOverlay>(Lifetime.Singleton).As<IPlatformOverlay>().AsSelf();
        }
    }
}
