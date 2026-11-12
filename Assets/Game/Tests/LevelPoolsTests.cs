using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AF.Core;
using HoldIt.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HoldIt.Tests
{
    public class LevelPoolsTests
    {
        private FakePools _root;
        private LevelPools _level;
        private Transform _marker;
        private Transform _bubble;

        [SetUp]
        public void SetUpPools()
        {
            _root = new FakePools();
            _level = new LevelPools(_root);
            _marker = new GameObject("Marker").transform;
            _bubble = new GameObject("Bubble").transform;
        }

        [TearDown]
        public void TearDownPools()
        {
            UnityEngine.Object.DestroyImmediate(_marker.gameObject);
            UnityEngine.Object.DestroyImmediate(_bubble.gameObject);
        }

        [Test]
        public void For_IsThePoolOfThePrefab()
        {
            Assert.AreSame(_marker, _level.For(_marker).Prefab);
        }

        [Test]
        public void For_TheSamePrefabTwice_IsTheSamePool()
        {
            Assert.AreSame(_level.For(_marker), _level.For(_marker));
        }

        [Test]
        public void CollectActive_HasWhatTheLevelSpawned()
        {
            _level.For(_marker).Spawn();
            _level.For(_bubble).Spawn();
            var found = new List<Transform>();

            _level.CollectActive(found);

            CollectionAssert.AreEquivalent(new[] { _marker, _bubble }, found);
        }

        [Test]
        public void CollectActive_LeavesOutWhatWasDespawned()
        {
            var pool = _level.For(_marker);
            var spawned = pool.Spawn();
            _level.For(_bubble).Spawn();
            pool.Despawn(spawned);
            var found = new List<Transform>();

            _level.CollectActive(found);

            CollectionAssert.AreEqual(new[] { _bubble }, found);
        }

        [Test]
        public void CollectActive_LeavesOutAnInstanceThatWentBackByItself()
        {
            _level.For(_marker).Spawn();
            // A puff despawns itself through its own life, not through the level's wrapper; the pool deactivates it.
            _marker.gameObject.SetActive(false);
            var found = new List<Transform>();

            _level.CollectActive(found);

            Assert.IsEmpty(found);
        }

        [Test]
        public void CollectActive_AfterDispose_IsEmpty()
        {
            _level.For(_marker).Spawn();
            _level.Dispose();
            var found = new List<Transform>();

            _level.CollectActive(found);

            Assert.IsEmpty(found);
        }

        [Test]
        public void Dispose_DespawnsEverythingOfEveryPoolTheLevelUsedOnce()
        {
            _level.For(_marker);
            _level.For(_marker);
            _level.For(_bubble);

            _level.Dispose();

            Assert.AreEqual(1, _root.Pool(_marker).DespawnAlls);
            Assert.AreEqual(1, _root.Pool(_bubble).DespawnAlls);
        }

        [Test]
        public void Dispose_LeavesAPoolTheLevelDidNotUse()
        {
            _level.For(_marker);
            var other = _root.Pool(_bubble);

            _level.Dispose();

            Assert.AreEqual(0, other.DespawnAlls);
        }

        [Test]
        public void Dispose_TakesBackWhatIsStillSpawned()
        {
            var pool = _level.For(_marker);
            pool.Spawn();
            pool.Spawn();

            _level.Dispose();

            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void Dispose_Twice_DespawnsOnce()
        {
            _level.For(_marker);

            _level.Dispose();
            _level.Dispose();

            Assert.AreEqual(1, _root.Pool(_marker).DespawnAlls);
        }

        [Test]
        public void For_AfterDispose_Throws()
        {
            _level.Dispose();

            Assert.Throws<ObjectDisposedException>(() => _level.For(_marker));
        }

        [Test]
        public void Dispose_APoolThatFails_DoesNotStopTheOthers()
        {
            _level.For(_marker);
            _level.For(_bubble);
            _root.Pool(_marker).FailOnDespawnAll = new InvalidOperationException("boom");
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            _level.Dispose();

            Assert.AreEqual(1, _root.Pool(_bubble).DespawnAlls);
        }

        [Test]
        public void ANewLevel_StartsWithNothingOfTheOldOnes()
        {
            _level.For(_marker);
            _level.Dispose();
            var next = new LevelPools(_root);
            next.For(_bubble);

            next.Dispose();

            Assert.AreEqual(1, _root.Pool(_marker).DespawnAlls, "The old level's pool is not despawned again.");
            Assert.AreEqual(1, _root.Pool(_bubble).DespawnAlls);
        }
    }
}
