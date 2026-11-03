using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>How the gameplay camera sees the board: where a world point lands on the screen, and which way it looks.</summary>
    public interface IBoardProjection
    {
        /// <summary>The camera's rotation, for things that turn to face it.</summary>
        Quaternion Rotation { get; }

        /// <summary>The screen position (pixels) of a world point. False when the point is behind the camera.</summary>
        bool TryWorldToScreen(Vector3 world, out Vector2 screen);
    }
}
