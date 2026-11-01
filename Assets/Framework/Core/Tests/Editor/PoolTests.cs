using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using MessagePipe;
using NUnit.Framework;
using PrimeTween;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using Object = UnityEngine.Object;

namespace AF.Core.Tests
{
    public class PoolTests
    {
        private IObjectResolver _container;
        private Pools _pools;
        private PoolProbe _prefab;
        private GameObject _outside;
        private FrameStamp _now;

        [SetUp]
        public void SetUp()
        {
            _container = BuildContainer(withPause: true);
            _now = new FrameStamp(100, 1f);
            _pools = new Pools(_container, null, () => _now);
            _prefab = new GameObject("Probe").AddComponent<PoolProbe>();
            _prefab.transform.localPosition = new Vector3(1f, 0f, 0f);
            _prefab.transform.localScale = new Vector3(2f, 2f, 2f);
            _outside = new GameObject("Outside");
        }

        [TearDown]
        public void TearDown()
        {
            _pools.Dispose();
            _container.Dispose();
            Object.DestroyImmediate(_prefab.gameObject);
            Object.DestroyImmediate(_outside);
        }

        [Test]
        public void Spawn_InjectsBeforeAwake_AndCallsTheHookAfterOnEnable()
        {
            var instance = _pools.For(_prefab).Spawn();

            CollectionAssert.AreEqual(new[] { "construct", "awake", "enable", "spawned" }, instance.Events);
            Assert.IsNotNull(instance.Pause);
            Assert.IsTrue(instance.gameObject.activeInHierarchy);
            Assert.IsTrue(instance.Life.IsCurrent);
        }

        [Test]
        public void CreatedInstances_WaitInactive_WithoutAwake()
        {
            var pool = _pools.For(_prefab);
            pool.Prewarm(2);

            Assert.AreEqual(2, pool.CountInactive);
            var waiting = Object.FindObjectsByType<PoolProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(probe => probe != _prefab)
                .ToList();
            Assert.AreEqual(2, waiting.Count);
            foreach (var probe in waiting)
            {
                CollectionAssert.AreEqual(new[] { "construct" }, probe.Events, "Injected, never activated.");
                Assert.IsFalse(probe.gameObject.activeSelf);
            }
        }

        [Test]
        public void InactivePrefabRoot_SpawnsActive()
        {
            _prefab.gameObject.SetActive(false);

            var instance = _pools.For(_prefab).Spawn();

            Assert.IsTrue(instance.gameObject.activeSelf);
            CollectionAssert.AreEqual(new[] { "construct", "awake", "enable", "spawned" }, instance.Events);
        }

        [Test]
        public void Despawn_EndsTheLifeBeforeTheHook_ThenDeactivates()
        {
            var pool = _pools.For(_prefab);
            var instance = pool.Spawn();
            var life = instance.Life;
            instance.Events.Clear();

            pool.Despawn(instance);

            CollectionAssert.AreEqual(new[] { "despawned, life cancelled: True", "disable" }, instance.Events);
            Assert.IsFalse(life.IsCurrent);
            Assert.IsTrue(life.Token.IsCancellationRequested);
            Assert.IsFalse(instance.gameObject.activeSelf);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreEqual("[Pools]", instance.transform.parent.parent.name);
        }

        [Test]
        public void Respawn_StartsANewLife_AndTheOldOneStaysDead()
        {
            var pool = _pools.For(_prefab);
            var first = pool.Spawn();
            var oldLife = first.Life;
            pool.Despawn(first);

            var second = pool.Spawn();

            Assert.AreSame(first, second, "The instance is reused.");
            Assert.IsFalse(oldLife.IsCurrent);
            Assert.IsFalse(oldLife.Despawn(), "A stale life does not despawn the new one.");
            Assert.AreEqual(1, pool.CountActive);
            Assert.IsTrue(second.Life.Despawn());
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void DespawnFromOnEnable_TakesEffectOnceActivated()
        {
            _prefab.DespawnOnEnable = true;
            var pool = _pools.For(_prefab);

            var instance = pool.Spawn(_outside.transform);

            CollectionAssert.AreEqual(new[] { "construct", "awake", "enable", "despawned, life cancelled: True", "disable" }, instance.Events,
                "No OnSpawned for a life that ended during activation.");
            Assert.IsFalse(instance.gameObject.activeSelf);
            Assert.AreNotEqual(_outside.transform, instance.transform.parent, "Back under the container.");
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);
        }

        [Test]
        public void HookThatDespawns_EndsTheSpawnForTheOtherHooks()
        {
            _prefab.DespawnOnSpawned = true;
            _prefab.gameObject.AddComponent<PoolProbe>();
            var pool = _pools.For(_prefab);

            var instance = pool.Spawn();

            var second = instance.GetComponents<PoolProbe>()[1];
            CollectionAssert.DoesNotContain(second.Events, "spawned");
            CollectionAssert.Contains(second.Events, "despawned, life cancelled: True");
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void DefaultLife_IsDead()
        {
            var life = default(PoolLife);

            Assert.IsFalse(life.IsCurrent);
            Assert.IsTrue(life.Token.IsCancellationRequested);
            Assert.IsFalse(life.Despawn());
        }

        [Test]
        public void DespawningTwice_Throws()
        {
            var pool = _pools.For(_prefab);
            var instance = pool.Spawn();
            pool.Despawn(instance);

            Assert.Throws<InvalidOperationException>(() => pool.Despawn(instance));
        }

        [Test]
        public void DespawningAnotherPoolsInstance_Throws()
        {
            var other = new GameObject("Other").AddComponent<PoolProbe>();
            try
            {
                var instance = _pools.For(other).Spawn();
                Assert.Throws<ArgumentException>(() => _pools.For(_prefab).Despawn(instance));
            }
            finally
            {
                Object.DestroyImmediate(other.gameObject);
            }
        }

        [Test]
        public void Spawn_ResetsTheTransform()
        {
            var pool = _pools.For(_prefab);
            var instance = pool.Spawn(_outside.transform);
            Assert.AreEqual(_outside.transform, instance.transform.parent);
            Assert.AreEqual(new Vector3(1f, 0f, 0f), instance.transform.localPosition);
            instance.transform.localScale = Vector3.one * 5f;
            pool.Despawn(instance);

            instance = pool.Spawn(new Vector3(0f, 3f, 0f), Quaternion.identity);

            Assert.IsNull(instance.transform.parent);
            Assert.AreEqual(new Vector3(0f, 3f, 0f), instance.transform.position);
            Assert.AreEqual(new Vector3(2f, 2f, 2f), instance.transform.localScale);
        }

        [Test]
        public void For_ReturnsOnePoolPerPrefab()
        {
            Assert.AreSame(_pools.For(_prefab), _pools.For(_prefab));
        }

        [Test]
        public void Dispose_DestroysEveryInstance_SpawnedOnesOutsideIncluded()
        {
            var pool = _pools.For(_prefab);
            var outside = pool.Spawn(_outside.transform);
            var life = outside.Life;
            pool.Prewarm(2);
            var container = GameObject.Find("[Pools]");

            _pools.Dispose();

            Assert.IsTrue(outside == null);
            Assert.IsTrue(life.Token.IsCancellationRequested);
            Assert.IsTrue(container == null);
            Assert.AreEqual(0, Object.FindObjectsByType<PoolProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(probe => probe != _prefab));
            Assert.Throws<ObjectDisposedException>(() => _pools.For(_prefab));
            Assert.DoesNotThrow(() => pool.Despawn(outside), "Despawning what the gone pool destroyed is harmless.");
        }

        [Test]
        public void DestroyedInstances_AreDropped()
        {
            var pool = _pools.For(_prefab);
            var spawned = pool.Spawn(_outside.transform);
            Object.DestroyImmediate(spawned.gameObject);

            Assert.DoesNotThrow(() => pool.Despawn(spawned));
            Assert.AreEqual(0, pool.CountActive);

            pool.Prewarm(1);
            var waiting = Object.FindObjectsByType<PoolProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(probe => probe != _prefab);
            Object.DestroyImmediate(waiting.gameObject);

            var fresh = pool.Spawn();
            Assert.IsTrue(fresh != null && fresh.gameObject.activeSelf, "A destroyed waiting instance is skipped.");
        }

        [Test]
        public void ScopedPools_InjectWithTheirOwnScope()
        {
            using var root = BuildContainer(withPause: false, scopedPools: true);
            var child = root.CreateScope(builder => RegisterPause(builder));

            var rootPools = root.Resolve<IPools>();
            var childPools = child.Resolve<IPools>();
            Assert.AreNotSame(rootPools, childPools);
            Assert.AreSame(childPools, child.Resolve<IPools>());

            var instance = childPools.For(_prefab).Spawn(_outside.transform);
            Assert.IsNotNull(instance.Pause, "Injected from the child, which alone registers the pause service.");

            child.Dispose();
            Assert.IsTrue(instance == null, "The child's pools went with it.");
        }

        [Test]
        public void TweenLeftFromAPreviousLife_IsReportedOnSpawn()
        {
            var pool = _pools.For(_prefab);
            var instance = pool.Spawn();
            var tween = Tween.Scale(instance.transform, 3f, 10f);
            try
            {
                pool.Despawn(instance);
                _now = new FrameStamp(_now.Frame + 2, _now.FixedTime + 0.02f);

                LogAssert.Expect(LogType.Error, new Regex("tween\\(s\\) from a previous life"));
                pool.Spawn();
            }
            finally
            {
                tween.Stop();
            }
        }

        [Test]
        public void TweenLeak_IsNotCheckedBeforeAFixedStep()
        {
            var pool = _pools.For(_prefab);
            var instance = pool.Spawn();
            var tween = Tween.Scale(instance.transform, 3f, 10f);
            try
            {
                pool.Despawn(instance);
                _now = new FrameStamp(_now.Frame + 5, _now.FixedTime);

                pool.Spawn();
            }
            finally
            {
                tween.Stop();
            }
        }

        [UnityTest]
        public IEnumerator TweenBoundToTheLife_StopsAfterDespawn()
        {
            var pool = _pools.For(_prefab);
            var instance = pool.Spawn();
            var tween = Tween.Scale(instance.transform, 3f, 10f).SetCancellationToken(instance.Life.Token);
            pool.Despawn(instance);

            var deadline = EditorApplication.timeSinceStartup + 2.0;
            while (tween.isAlive && EditorApplication.timeSinceStartup < deadline)
                yield return null;

            Assert.IsFalse(tween.isAlive);
            _now = new FrameStamp(_now.Frame + 2, _now.FixedTime + 0.02f);
            pool.Spawn();
        }

        [UnityTest]
        public IEnumerator TweenOfAnEndedLife_NeverMovesTheNextLife()
        {
            var pool = _pools.For(_prefab);
            var instance = pool.Spawn(Vector3.zero, Quaternion.identity);
            var tween = Tween.PositionX(instance.transform, 10f, 5f).SetCancellationToken(instance.Life.Token);
            pool.Despawn(instance);

            var next = pool.Spawn(Vector3.zero, Quaternion.identity);
            Assert.AreSame(instance, next, "Respawned in the same frame, before PrimeTween saw the cancellation.");

            var deadline = EditorApplication.timeSinceStartup + 0.3;
            while (EditorApplication.timeSinceStartup < deadline)
                yield return null;

            Assert.IsFalse(tween.isAlive);
            Assert.AreEqual(0f, next.transform.position.x);
        }

        private static IObjectResolver BuildContainer(bool withPause, bool scopedPools = false)
        {
            var builder = new ContainerBuilder();
            if (withPause)
                RegisterPause(builder);
            if (scopedPools)
                builder.Register<IPools, Pools>(Lifetime.Scoped);
            return builder.Build();
        }

        private static void RegisterPause(IContainerBuilder builder)
        {
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<PauseChanged>(messagePipe);
            builder.Register<IPauseService, PauseService>(Lifetime.Singleton);
        }
    }
}
