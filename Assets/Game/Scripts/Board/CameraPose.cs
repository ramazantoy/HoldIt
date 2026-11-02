using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>Where a camera stands and how it is turned (<see cref="CameraFit"/>).</summary>
    public readonly struct CameraPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public CameraPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
