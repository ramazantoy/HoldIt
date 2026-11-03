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

        public PathMarker Marker => _marker;
        public float PickRadius => _pickRadius;
        public float PickHeight => _pickHeight;
        public float WalkedMarkHeight => _walkedMarkHeight;
        public float DrawnMarkHeight => _drawnMarkHeight;
    }
}
