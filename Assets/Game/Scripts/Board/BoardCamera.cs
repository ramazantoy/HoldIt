using System;
using AF.Core;
using UnityEngine;
using VContainer;

namespace HoldIt.Board
{
    /// <summary>
    /// Keeps the gameplay camera fitted to the board (<see cref="CameraFit"/>): inside the safe area, below the HUD's top bar,
    /// less the config's margins. Refits whenever the screen, the safe area, the insets, the board or the config changes;
    /// Android can report the insets late. Also says which tile is under a screen point.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCamera : MonoBehaviour, IBoardPicker
    {
        [SerializeField, Required] private BoardCameraConfig _config;
        [SerializeField, Required] private Camera _camera;

        private readonly Vector3[] _corners = new Vector3[8];
        private ITileGrid _grid;
        private IBoardBounds _board;
        private IBoardInsets _insets;
        private FitInput _applied;
        private bool _hasApplied;

        [Inject]
        public void Construct(ITileGrid grid, IBoardBounds board, IBoardInsets insets)
        {
            _grid = grid;
            _board = board;
            _insets = insets;
        }

        public bool TryPick(Vector2 screenPosition, out Cell cell)
        {
            cell = default;
            return BoardSpace.TryHitFloor(_camera.ScreenPointToRay(screenPosition), out var point) &&
                   _grid.Space.TryGetCell(point, out cell);
        }

        private void LateUpdate()
        {
            if (!_board.IsBuilt || Screen.width <= 0 || Screen.height <= 0)
                return;

            var input = new FitInput(Screen.width, Screen.height, Screen.safeArea, _insets.Top, _board.Bounds,
                _config.Tilt, _config.FieldOfView, _config.SideMargin, _config.BottomMargin, _config.TopMargin);
            if (_hasApplied && input.Equals(_applied))
                return;

            _applied = input;
            _hasApplied = true;
            Fit(input);
        }

        private void Fit(FitInput input)
        {
            var target = TargetRect(input);
            if (target.width <= 0f || target.height <= 0f)
            {
                Log.Warning($"No room left for the board: safe area {input.SafeArea}, top inset {input.TopInset}.");
                return;
            }

            FillCorners(input.Bounds);
            var pose = CameraFit.Fit(_corners, _camera.aspect, input.FieldOfView, input.Tilt, target);
            _camera.fieldOfView = input.FieldOfView;
            transform.SetPositionAndRotation(pose.Position, pose.Rotation);
        }

        private static Rect TargetRect(FitInput input)
        {
            var width = (float)input.Width;
            var height = (float)input.Height;
            var top = Mathf.Min(input.SafeArea.yMax, height - input.TopInset);
            return Rect.MinMaxRect(
                input.SafeArea.xMin / width + input.SideMargin,
                input.SafeArea.yMin / height + input.BottomMargin,
                input.SafeArea.xMax / width - input.SideMargin,
                top / height - input.TopMargin);
        }

        private void FillCorners(Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            for (var i = 0; i < 8; i++)
            {
                _corners[i] = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
            }
        }

        private readonly struct FitInput : IEquatable<FitInput>
        {
            public readonly int Width;
            public readonly int Height;
            public readonly Rect SafeArea;
            public readonly float TopInset;
            public readonly Bounds Bounds;
            public readonly float Tilt;
            public readonly float FieldOfView;
            public readonly float SideMargin;
            public readonly float BottomMargin;
            public readonly float TopMargin;

            public FitInput(int width, int height, Rect safeArea, float topInset, Bounds bounds, float tilt,
                float fieldOfView, float sideMargin, float bottomMargin, float topMargin)
            {
                Width = width;
                Height = height;
                SafeArea = safeArea;
                TopInset = topInset;
                Bounds = bounds;
                Tilt = tilt;
                FieldOfView = fieldOfView;
                SideMargin = sideMargin;
                BottomMargin = bottomMargin;
                TopMargin = topMargin;
            }

            public bool Equals(FitInput other) =>
                Width == other.Width && Height == other.Height && SafeArea == other.SafeArea &&
                TopInset.Equals(other.TopInset) && Bounds == other.Bounds && Tilt.Equals(other.Tilt) &&
                FieldOfView.Equals(other.FieldOfView) && SideMargin.Equals(other.SideMargin) &&
                BottomMargin.Equals(other.BottomMargin) && TopMargin.Equals(other.TopMargin);

            public override bool Equals(object obj) => obj is FitInput other && Equals(other);
            public override int GetHashCode() => (Width * 397) ^ Height;
        }
    }
}
