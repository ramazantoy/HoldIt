using PrimeTween;
using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>
    /// How a level's board comes in and goes out (<see cref="RevealWave"/>): its pieces grow in a wave from the top left
    /// corner, a step after one another, and shrink the same way as the level changes.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Board/Board Reveal Config", fileName = "BoardRevealConfig")]
    public sealed class BoardRevealConfig : ScriptableObject
    {
        [SerializeField, Range(0f, 0.5f), Tooltip("Seconds the wave takes per tile of the board's width plus depth.")]
        private float _step = 0.1f;

        [SerializeField, Range(0f, 5f), Tooltip("The longest the wave takes to cross, whatever the map's size.")]
        private float _maxSeconds = 1.2f;

        [SerializeField, Range(0.05f, 2f), Tooltip("Seconds one piece takes to grow to its size.")]
        private float _pieceSeconds = 0.25f;

        [SerializeField, Tooltip("How a piece eases as it grows.")]
        private Ease _ease = Ease.OutBack;

        [SerializeField, Range(1f, 5f), Tooltip("How much faster the board goes out than it comes in.")]
        private float _outSpeed = 2f;

        public float Step => _step;
        public float MaxSeconds => _maxSeconds;
        public float PieceSeconds => _pieceSeconds;
        public Ease Ease => _ease;
        public float OutSpeed => _outSpeed;
    }
}
