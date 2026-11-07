using System;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Restrooms
{
    /// <summary>Builds the level's restrooms in the scene's <see cref="RestroomView"/> as the level opens, and takes them down as it ends.</summary>
    internal sealed class RestroomBuilder : IInitializable, IDisposable
    {
        private readonly RestroomView _view;
        private readonly IRestrooms _restrooms;
        private readonly StallSet _set;
        private readonly IObjectResolver _resolver;
        private readonly ISubscriber<StallStateChanged> _changed;

        public RestroomBuilder(RestroomView view, IRestrooms restrooms, StallSet set, IObjectResolver resolver,
            ISubscriber<StallStateChanged> changed)
        {
            _view = view;
            _restrooms = restrooms;
            _set = set;
            _resolver = resolver;
            _changed = changed;
        }

        public void Initialize() => _view.Build(_restrooms, _set, _resolver, _changed);

        public void Dispose()
        {
            if (_view != null)
                _view.Clear();
        }
    }
}
