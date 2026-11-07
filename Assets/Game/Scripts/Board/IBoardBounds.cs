using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>
    /// The board that stands now: the grid it was built from and the space it takes up, so the camera can fit it whatever
    /// its pieces look like. It outlives a level: the next level's board replaces it.
    /// </summary>
    public interface IBoardBounds
    {
        /// <summary>False while no board's pieces exist.</summary>
        bool IsBuilt { get; }

        /// <summary>The grid the standing board was built from; null while <see cref="IsBuilt"/> is false.</summary>
        ITileGrid Grid { get; }

        /// <summary>World bounds of every renderer on the board.</summary>
        Bounds Bounds { get; }
    }
}
