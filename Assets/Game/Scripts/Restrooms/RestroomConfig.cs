using UnityEngine;

namespace HoldIt.Restrooms
{
    /// <summary>How long a stall is busy and where the stalls stand. How many a restroom has is the map's.</summary>
    [CreateAssetMenu(menuName = "HoldIt/Restrooms/Restroom Config", fileName = "RestroomConfig")]
    public sealed class RestroomConfig : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("How long a stall stays busy from the moment a visitor takes it, in seconds. It frees once that time is up and the visitor is in.")]
        private float _stallSeconds = 4.5f;

        [SerializeField, Tooltip("Where the stalls stand in a restroom, and the way a visitor walks into one.")]
        private StallGeometry _geometry = StallGeometry.Default;

        public float StallSeconds => _stallSeconds;
        public StallGeometry Geometry => _geometry;
    }
}
