using PrimeTween;
using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>
    /// How a level's board comes in and goes out (<see cref="RevealWave"/>): its pieces grow in a wave from the top left
    /// corner and take the same time to cross whatever the map, and shrink the same way as the level changes.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Board/Board Reveal Config", fileName = "BoardRevealConfig")]
    public sealed class BoardRevealConfig : ScriptableObject
    {
        [SerializeField, Range(0.1f, 10f), Tooltip("Seconds the wave takes to cross the board, whatever the map's size: a bigger map's tiles follow one another faster.")]
        private float _waveSeconds = 5f;

        [SerializeField, Range(0.05f, 2f), Tooltip("Seconds one piece takes to grow to its size.")]
        private float _pieceSeconds = 0.25f;

        [SerializeField, Tooltip("How a piece eases as it grows.")]
        private Ease _ease = Ease.OutBack;

        [SerializeField, Range(1f, 5f), Tooltip("How much faster the board goes out than it comes in.")]
        private float _outSpeed = 2f;

        public float WaveSeconds => _waveSeconds;
        public float PieceSeconds => _pieceSeconds;
        public Ease Ease => _ease;
        public float OutSpeed => _outSpeed;
    }
}
