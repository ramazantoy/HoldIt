using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>Which tile is under a point of the screen.</summary>
    public interface IBoardPicker
    {
        /// <summary>The cell whose floor is under the screen position (pixels). False off the board.</summary>
        bool TryPick(Vector2 screenPosition, out Cell cell);

        /// <summary>
        /// Like <see cref="TryPick"/>, but a point off the board gives the nearest edge cell. False only when the screen
        /// position does not look at the floor at all.
        /// </summary>
        bool TryPickClamped(Vector2 screenPosition, out Cell cell);
    }
}
