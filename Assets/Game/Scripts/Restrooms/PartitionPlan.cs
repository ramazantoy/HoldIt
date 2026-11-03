using UnityEngine;

namespace HoldIt.Restrooms
{
    /// <summary>One straight piece of partition: its middle on the floor, its run along local +Z, and how long it is.</summary>
    public readonly struct PartitionPlan
    {
        public readonly Vector3 Center;
        public readonly Quaternion Rotation;
        public readonly float Length;

        public PartitionPlan(Vector3 center, Quaternion rotation, float length)
        {
            Center = center;
            Rotation = rotation;
            Length = length;
        }
    }
}
