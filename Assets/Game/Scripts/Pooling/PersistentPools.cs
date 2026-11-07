using System;
using AF.Core;
using UnityEngine;
using VContainer;

namespace HoldIt.Pooling
{
    /// <summary>
    /// The root's own <see cref="Pools"/>, built with the root's resolver. It is not the root's scoped <see cref="IPools"/>:
    /// no root service asks for that one, and this one is named for what it is.
    /// </summary>
    internal sealed class PersistentPools : IPersistentPools, IDisposable
    {
        private readonly Pools _pools;

        public PersistentPools(IObjectResolver resolver) => _pools = new Pools(resolver);

        public IPool<T> For<T>(T prefab) where T : Component => _pools.For(prefab);

        public void Dispose() => _pools.Dispose();
    }
}
