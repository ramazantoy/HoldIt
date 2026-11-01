using UnityEngine;

namespace AF.Core
{
    /// <summary>
    /// The prefab pools of one scope, the counterpart of the MemoryPools a Zenject installer binds. Every scope resolves its
    /// own: a scene's objects and services get the scene's pools, root services get the root's. A pool injects its instances
    /// with its scope's registrations and destroys all of them, spawned ones included, when its scope is disposed.
    /// </summary>
    public interface IPools
    {
        /// <summary>
        /// The pool of <paramref name="prefab"/> in this scope, created on the first call. Pass the same component of the prefab
        /// every time: another component, or the same one as another type, gets a pool of its own.
        /// </summary>
        IPool<T> For<T>(T prefab) where T : Component;
    }

    /// <summary>
    /// Spawns copies of one prefab and takes them back. An instance waits in the pool inactive, under the scope's
    /// <c>[Pools]</c> object; it is injected when it is created, before its first Awake.
    /// </summary>
    public interface IPool<T> where T : Component
    {
        T Prefab { get; }

        /// <summary>Spawned instances that have not been despawned.</summary>
        int CountActive { get; }

        /// <summary>Instances waiting in the pool.</summary>
        int CountInactive { get; }

        /// <summary>
        /// Takes an instance under <paramref name="parent"/> (null: the root of the pool's scene) with the prefab's local
        /// position, rotation and scale, then activates it.
        /// </summary>
        T Spawn(Transform parent = null);

        /// <summary>Takes an instance at a world position and rotation, with the prefab's scale, then activates it.</summary>
        T Spawn(Vector3 position, Quaternion rotation, Transform parent = null);

        /// <summary>
        /// Ends the instance's life and puts it back. Throws when it is not spawned from this pool; an instance destroyed
        /// meanwhile is ignored. To despawn only if the instance still lives the same life, use <see cref="PoolLife.Despawn"/>.
        /// Called from the instance's Awake or OnEnable, it takes effect once the activation ends. Do not call it from
        /// OnDisable: Unity refuses to move an object while it is being deactivated.
        /// </summary>
        void Despawn(T instance);

        void DespawnAll();

        /// <summary>Creates instances until the pool holds at least <paramref name="count"/> inactive ones.</summary>
        void Prewarm(int count);
    }
}
