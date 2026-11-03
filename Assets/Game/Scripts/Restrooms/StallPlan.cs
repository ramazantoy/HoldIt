using HoldIt.Paths;
using UnityEngine;

namespace HoldIt.Restrooms
{
    /// <summary>
    /// Where one stall's parts stand, in world space. Every part turns by <see cref="Rotation"/>: modelled facing +Z, they
    /// then face out of the restroom, toward its door.
    /// </summary>
    public readonly struct StallPlan
    {
        /// <summary>Where the door hangs, on the side of the opening that comes first along the restroom.</summary>
        public readonly Vector3 Hinge;

        /// <summary>On top of the front partition, between the door and the stall's far side.</summary>
        public readonly Vector3 Lamp;

        /// <summary>Against the back wall, in the middle of the stall.</summary>
        public readonly Vector3 Toilet;

        public readonly Quaternion Rotation;

        /// <summary>From the centre of the restroom's door to where the visitor ends up inside the stall.</summary>
        public readonly WalkLine Entry;

        public StallPlan(Vector3 hinge, Vector3 lamp, Vector3 toilet, Quaternion rotation, WalkLine entry)
        {
            Hinge = hinge;
            Lamp = lamp;
            Toilet = toilet;
            Rotation = rotation;
            Entry = entry;
        }
    }
}
