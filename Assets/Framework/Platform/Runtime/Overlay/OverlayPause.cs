using System;
using AF.Core;
using MessagePipe;
using VContainer.Unity;

namespace AF.Platform
{
    /// <summary>Holds a pause request while the store overlay is open. Registered by <c>UsePlatform</c> when <see cref="PlatformOptions.PauseOnOverlay"/> is on.</summary>
    internal sealed class OverlayPause : IInitializable, IDisposable
    {
        private const string PAUSE_REASON = "platform-overlay";

        private readonly IPauseService _pause;
        private readonly IPlatformOverlay _overlay;
        private readonly ISubscriber<PlatformOverlayChanged> _overlayChanged;
        private IDisposable _subscription;
        private IDisposable _pauseHandle;

        public OverlayPause(IPauseService pause, IPlatformOverlay overlay, ISubscriber<PlatformOverlayChanged> overlayChanged)
        {
            _pause = pause;
            _overlay = overlay;
            _overlayChanged = overlayChanged;
        }

        public void Initialize()
        {
            _subscription = _overlayChanged.Subscribe(change => Apply(change.IsOpen));
            Apply(_overlay.IsOpen);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            Apply(false);
        }

        private void Apply(bool open)
        {
            if (open && _pauseHandle == null)
            {
                _pauseHandle = _pause.Request(PAUSE_REASON);
            }
            else if (!open && _pauseHandle != null)
            {
                _pauseHandle.Dispose();
                _pauseHandle = null;
            }
        }
    }
}
