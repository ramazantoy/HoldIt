using System;
using UnityEngine;

namespace HoldIt.Restrooms
{
    /// <summary>
    /// How the stalls sit in a restroom (<see cref="RestroomLayout"/>) and the way a visitor walks into one. Lengths are in
    /// tiles; depths are measured from the outer face of the restroom's back wall, the side across from its door.
    /// </summary>
    [Serializable]
    public struct StallGeometry
    {
        [SerializeField, Min(0.5f), Tooltip("Where the stalls' front partition stands, measured from the back wall's outer face.")]
        private float _stallDepth;

        [SerializeField, Min(0.1f), Tooltip("How wide a stall's door opening is.")]
        private float _doorWidth;

        [SerializeField, Min(0.01f), Tooltip("How thick a partition is; the restroom's own walls are as thick.")]
        private float _wallThickness;

        [SerializeField, Min(0f), Tooltip("The least room a stall keeps on each side of its door, for the partition and the lamp.")]
        private float _sideRoom;

        [SerializeField, Min(0f), Tooltip("How far past the reach of an open stall door a visitor walks along, on its way to its own stall.")]
        private float _approach;

        [SerializeField, Min(0f), Tooltip("How far past the partition a visitor walks before heading for the back.")]
        private float _pass;

        [SerializeField, Min(0.1f), Tooltip("Where a visitor ends up in its stall, measured from the back wall's outer face.")]
        private float _insideDepth;

        public StallGeometry(float stallDepth, float doorWidth, float wallThickness, float sideRoom,
            float approach, float pass, float insideDepth)
        {
            _stallDepth = stallDepth;
            _doorWidth = doorWidth;
            _wallThickness = wallThickness;
            _sideRoom = sideRoom;
            _approach = approach;
            _pass = pass;
            _insideDepth = insideDepth;
        }

        /// <summary>The prototype's stalls: 1.8 deep with a 0.9 door; the restroom wall is the kit's 0.14.</summary>
        public static StallGeometry Default => new StallGeometry(1.8f, 0.9f, 0.14f, 0.3f, 0.4f, 0.15f, 0.95f);

        public float StallDepth => _stallDepth;
        public float DoorWidth => _doorWidth;
        public float WallThickness => _wallThickness;
        public float SideRoom => _sideRoom;
        public float Approach => _approach;
        public float Pass => _pass;
        public float InsideDepth => _insideDepth;

        /// <summary>
        /// Where a visitor walks along in front of the stalls, from the back wall's outer face: the front partition, the
        /// door's length (an open door swings out about that far) and <see cref="Approach"/> beyond it. It keeps clear of
        /// every open stall door whichever stall the visitor is bound for.
        /// </summary>
        public float LaneDepth => _stallDepth + _doorWidth + _approach;

        /// <summary>The narrowest a stall can be: its door with room on both sides.</summary>
        public float MinStallWidth => _doorWidth + 2f * _sideRoom;
    }
}
