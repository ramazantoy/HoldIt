using System;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace AF.Core
{
    // Ticks in VContainer's Update stage, which runs before MonoBehaviour.Update, so scripts read this frame's values.
    public sealed class GameClock : IGameClock, ITickable, IDisposable
    {
        private readonly IPauseService _pause;
        private readonly IDisposable _subscription;
        private readonly Action<float> _applyTimeScale;
        private float _scale = 1f;

        [Inject]
        public GameClock(IPauseService pause, ISubscriber<PauseChanged> pauseChanged)
            : this(pause, pauseChanged, value => UnityEngine.Time.timeScale = value)
        {
        }

        // Tests pass their own sink so they never touch the global UnityEngine.Time.timeScale.
        internal GameClock(IPauseService pause, ISubscriber<PauseChanged> pauseChanged, Action<float> applyTimeScale)
        {
            _pause = pause;
            _applyTimeScale = applyTimeScale;
            _subscription = pauseChanged.Subscribe(_ => ApplyTimeScale());
            ApplyTimeScale();
        }

        public float Time { get; private set; }
        public float DeltaTime { get; private set; }

        public float Scale
        {
            get => _scale;
            set
            {
                if (value < 0f || float.IsNaN(value))
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Scale must be zero or positive.");
                _scale = value;
                ApplyTimeScale();
            }
        }

        public void Tick() => Advance(UnityEngine.Time.deltaTime);

        internal void Advance(float scaledDeltaTime)
        {
            DeltaTime = _pause.IsPaused ? 0f : scaledDeltaTime;
            Time += DeltaTime;
        }

        public void Dispose()
        {
            _subscription.Dispose();
            _applyTimeScale(1f);
        }

        private void ApplyTimeScale() => _applyTimeScale(_pause.IsPaused ? 0f : _scale);
    }
}
