using AF.Core;

namespace HoldIt.Pooling
{
    /// <summary>
    /// What a level spawns from, in the level's scope: the root's pools, with a record of the ones the level used. When the
    /// level goes, everything it still has spawned goes back to them (<see cref="LevelPools"/>). Only one level lives at a
    /// time, so a pool is the level's alone.
    /// </summary>
    public interface ILevelPools : IPools
    {
    }
}
