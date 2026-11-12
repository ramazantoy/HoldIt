using System.Collections.Generic;
using HoldIt.Pooling;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>A level's child scope as an <see cref="ILevel"/>: what stands in it is what its pools have spawned.</summary>
    internal sealed class LevelScope : ILevel
    {
        private readonly LifetimeScope _scope;
        private readonly ILevelPools _pools;
        private bool _disposed;

        public LevelScope(LifetimeScope scope)
        {
            _scope = scope;
            _pools = scope.Container.Resolve<ILevelPools>();
        }

        public void CollectContent(List<Transform> into)
        {
            if (!_disposed)
                _pools.CollectActive(into);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _scope.Dispose();
        }
    }
}
