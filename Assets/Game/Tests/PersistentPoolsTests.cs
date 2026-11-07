using HoldIt.Pooling;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace HoldIt.Tests
{
    public class PersistentPoolsTests
    {
        private PersistentService _service;
        private IObjectResolver _root;
        private IPersistentPools _pools;
        private PersistentProbe _prefab;

        [SetUp]
        public void SetUpPools()
        {
            _service = new PersistentService();
            var builder = new ContainerBuilder();
            builder.RegisterInstance(_service);
            builder.Register<PersistentPools>(Lifetime.Singleton).As<IPersistentPools>();
            _root = builder.Build();
            _pools = _root.Resolve<IPersistentPools>();
            _prefab = new GameObject("Probe").AddComponent<PersistentProbe>();
        }

        [TearDown]
        public void TearDownPools()
        {
            _root.Dispose();
            Object.DestroyImmediate(_prefab.gameObject);
        }

        [Test]
        public void Instance_IsInjectedWithTheRootsServicesOnceItIsMade()
        {
            var pool = _pools.For(_prefab);

            pool.Prewarm(1);

            Assert.AreEqual(1, pool.CountInactive);
            var instance = pool.Spawn();
            Assert.AreSame(_service, instance.Service);
            Assert.AreEqual(1, instance.Spawned);
        }

        [Test]
        public void Level_GivesTheInstancesBackAndTheNextLevelSpawnsTheSameOnes()
        {
            var first = new LevelPools(_pools);
            var instance = first.For(_prefab).Spawn();
            first.Dispose();

            var pool = _pools.For(_prefab);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);
            Assert.IsTrue(instance != null, "The level going does not destroy what the root pooled.");
            Assert.IsFalse(instance.gameObject.activeSelf);
            Assert.AreEqual(1, instance.Despawned);

            var second = new LevelPools(_pools);
            var again = second.For(_prefab).Spawn();
            Assert.AreSame(instance, again);
            Assert.AreEqual(2, again.Spawned);
            second.Dispose();
        }

        [Test]
        public void Dispose_DestroysTheInstances()
        {
            var instance = _pools.For(_prefab).Spawn();

            ((System.IDisposable)_pools).Dispose();

            Assert.IsTrue(instance == null);
        }
    }
}
