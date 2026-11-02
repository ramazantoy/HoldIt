using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>
    /// How the gameplay camera looks at the board (<see cref="BoardCamera"/>). The board is fitted into the safe area below
    /// the HUD's top bar, less these margins.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Board/Board Camera Config", fileName = "BoardCameraConfig")]
    public sealed class BoardCameraConfig : ScriptableObject
    {
        [SerializeField, Range(0f, 80f), Tooltip("Degrees away from looking straight down; the camera looks toward the map's top row.")]
        private float _tilt = 22f;

        [SerializeField, Range(10f, 90f), Tooltip("Vertical field of view in degrees.")]
        private float _fieldOfView = 40f;

        [SerializeField, Range(0f, 0.2f), Tooltip("Gap left and right of the board, as a part of the screen width.")]
        private float _sideMargin = 0.02f;

        [SerializeField, Range(0f, 0.2f), Tooltip("Gap below the board, as a part of the screen height.")]
        private float _bottomMargin = 0.02f;

        [SerializeField, Range(0f, 0.2f), Tooltip("Gap between the HUD's top bar and the board, as a part of the screen height.")]
        private float _topMargin = 0.01f;

        public float Tilt => _tilt;
        public float FieldOfView => _fieldOfView;
        public float SideMargin => _sideMargin;
        public float BottomMargin => _bottomMargin;
        public float TopMargin => _topMargin;
    }
}
