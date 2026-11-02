using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>Which tile is under a point of the screen.</summary>
    public interface IBoardPicker
    {
        /// <summary>The cell whose floor is under the screen position (pixels). False off the board.</summary>
        bool TryPick(Vector2 screenPosition, out Cell cell);
    }
}
