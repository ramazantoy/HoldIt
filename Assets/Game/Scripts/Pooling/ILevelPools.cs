using System.Collections.Generic;
using AF.Core;
using UnityEngine;

namespace HoldIt.Pooling
{
    /// <summary>
    /// What a level spawns from, in the level's scope: the root's pools, with a record of the ones the level used. When the
    /// level goes, everything it still has spawned goes back to them (<see cref="LevelPools"/>). Only one level lives at a
    /// time, so a pool is the level's alone.
    /// </summary>
    public interface ILevelPools : IPools
    {
        /// <summary>Adds the transform of everything the level has spawned that is on the board now.</summary>
        void CollectActive(List<Transform> into);
    }
}
