using System;
using System.Collections.Generic;
using MessagePipe;

namespace AF.Core
{
    public sealed class PauseService : IPauseService
    {
        private readonly IPublisher<PauseChanged> _pauseChanged;
        private readonly List<Handle> _handles = new List<Handle>();
        private readonly List<string> _reasons = new List<string>();

        public PauseService(IPublisher<PauseChanged> pauseChanged) => _pauseChanged = pauseChanged;

        public bool IsPaused => _handles.Count > 0;
        public IReadOnlyList<string> ActiveReasons => _reasons;

        public IDisposable Request(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentException("A pause request needs a reason.", nameof(reason));

            var handle = new Handle(this);
            _handles.Add(handle);
            _reasons.Add(reason);
            if (_handles.Count == 1)
                _pauseChanged.Publish(new PauseChanged(true));
            return handle;
        }

        private void Release(Handle handle)
        {
            var index = _handles.IndexOf(handle);
            if (index < 0)
                return;

            _handles.RemoveAt(index);
            _reasons.RemoveAt(index);
            if (_handles.Count == 0)
                _pauseChanged.Publish(new PauseChanged(false));
        }

        private sealed class Handle : IDisposable
        {
            private PauseService _owner;

            public Handle(PauseService owner) => _owner = owner;

            public void Dispose()
            {
                var releasing = _owner;
                _owner = null;
                releasing?.Release(this);
            }
        }
    }
}
