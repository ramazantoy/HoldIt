using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>The space the built board takes up, so the camera can fit it whatever its pieces look like.</summary>
    public interface IBoardBounds
    {
        /// <summary>False until the board's pieces exist.</summary>
        bool IsBuilt { get; }

        /// <summary>World bounds of every renderer on the board.</summary>
        Bounds Bounds { get; }
    }
}
