using AF.Core;
using UnityEngine;

namespace HoldIt.Drawing
{
    /// <summary>How the player picks a character and how the drawn path is shown.</summary>
    [CreateAssetMenu(menuName = "HoldIt/Drawing/Drawing Config", fileName = "DrawingConfig")]
    public sealed class DrawingConfig : ScriptableObject
    {
        [SerializeField, Required, Tooltip("The mark on each cell of a drawn or walked path.")]
        private PathMarker _marker;

        [SerializeField, Min(0.1f), Tooltip("How far from a character a press still picks it, in tiles.")]
        private float _pickRadius = 0.75f;

        [SerializeField, Min(0f), Tooltip("Where a character's head is, in tiles above the floor, for picking.")]
        private float _pickHeight = 1.1f;

        [SerializeField, Min(0f), Tooltip("How high above the floor the marks of a walked path lie, in tiles.")]
        private float _walkedMarkHeight = 0.02f;

        [SerializeField, Min(0f), Tooltip("How high above the floor the marks of the path being drawn lie, in tiles.")]
        private float _drawnMarkHeight = 0.03f;

        [SerializeField, Min(0.01f), Tooltip("How long a new mark takes to pop in, in seconds.")]
        private float _popSeconds = 0.18f;

        [SerializeField, Min(0.01f), Tooltip("How long a removed mark takes to shrink away, in seconds.")]
        private float _vanishSeconds = 0.14f;

        [SerializeField, Min(0.01f), Tooltip("How long a mark shakes when the path cannot end there, in seconds.")]
        private float _shakeSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("How far the shake moves a mark, in tiles.")]
        private float _shakeStrength = 0.08f;

        [SerializeField, Min(1f), Tooltip("Shakes per second.")]
        private float _shakeFrequency = 14f;

        public PathMarker Marker => _marker;
        public float PopSeconds => _popSeconds;
        public float VanishSeconds => _vanishSeconds;
        public float ShakeSeconds => _shakeSeconds;
        public float ShakeStrength => _shakeStrength;
        public float ShakeFrequency => _shakeFrequency;
        public float PickRadius => _pickRadius;
        public float PickHeight => _pickHeight;
        public float WalkedMarkHeight => _walkedMarkHeight;
        public float DrawnMarkHeight => _drawnMarkHeight;
    }
}
