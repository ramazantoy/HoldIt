using MessagePipe;

namespace AF.Platform
{
    /// <summary>An overlay that only opens when told to, for tests and for trying the game's reaction in Play Mode.</summary>
    public sealed class MockPlatformOverlay : IPlatformOverlay
    {
        private readonly IPublisher<PlatformOverlayChanged> _changed;

        public MockPlatformOverlay(IPublisher<PlatformOverlayChanged> changed) => _changed = changed;

        public bool IsOpen { get; private set; }

        public void SetOpen(bool open)
        {
            if (open == IsOpen)
                return;
            IsOpen = open;
            _changed.Publish(new PlatformOverlayChanged(open));
        }
    }
}
