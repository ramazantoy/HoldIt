namespace AF.Core
{
    /// <summary>
    /// Spawn and despawn hooks for components of a pooled prefab. Both run outside Unity's activation callbacks, so they can
    /// reset state and toggle children (Unity ignores <c>SetActive</c> on children from inside OnEnable or OnDisable).
    /// Subscriptions still belong in OnEnable and OnDisable.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// After the instance is activated. Pass <paramref name="life"/>'s token to every awaited operation and tween of this
        /// life: it is cancelled when the instance is despawned.
        /// </summary>
        void OnSpawned(PoolLife life);

        /// <summary>After the life's token is cancelled, before the instance is deactivated.</summary>
        void OnDespawned();
    }
}
