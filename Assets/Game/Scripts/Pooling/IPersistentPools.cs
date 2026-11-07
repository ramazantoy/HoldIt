using AF.Core;

namespace HoldIt.Pooling
{
    /// <summary>
    /// The pools of the root scope: they outlast the scenes and the levels, so the views are made once (while the intro plays,
    /// see <see cref="PoolWarmupStep"/>) and kept. An instance is injected once, with the root's registrations only: what
    /// a level or a scene offers is handed to the view when it is spawned, and let go when it is despawned.
    /// </summary>
    public interface IPersistentPools : IPools
    {
    }
}
