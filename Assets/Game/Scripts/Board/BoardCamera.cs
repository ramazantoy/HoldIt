using System;
using AF.Core;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace HoldIt.Board
{
    /// <summary>
    /// Keeps the gameplay camera fitted to the board (<see cref="CameraFit"/>): inside the safe area, below the HUD's top bar,
    /// less the config's margins. Refits whenever the screen, the safe area, the insets, the board or the config changes;
    /// Android can report the insets late. When only the board changes (the next level's), the camera slides to the new fit
    /// instead of jumping. Also says which tile is under a screen point, and where a world point lands.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCamera : MonoBehaviour, IBoardPicker, IBoardProjection
    {
        [SerializeField, Required] private BoardCameraConfig _config;
        [SerializeField, Required] private Camera _camera;

        private readonly Vector3[] _corners = new Vector3[8];
        private IBoardBounds _board;
        private IBoardInsets _insets;
        private FitInput _applied;
        private bool _hasApplied;
        private Tween _slide;

        [Inject]
        public void Construct(IBoardBounds board, IBoardInsets insets)
        {
            _board = board;
            _insets = insets;
            _hasApplied = false;
        }

        public Quaternion Rotation => transform.rotation;

        public bool TryPick(Vector2 screenPosition, out Cell cell)
        {
            cell = default;
            return _board.IsBuilt && BoardSpace.TryHitFloor(_camera.ScreenPointToRay(screenPosition), out var point) &&
                   _board.Grid.Space.TryGetCell(point, out cell);
        }

        public bool TryPickClamped(Vector2 screenPosition, out Cell cell)
        {
            cell = default;
            if (!_board.IsBuilt || !BoardSpace.TryHitFloor(_camera.ScreenPointToRay(screenPosition), out var point))
                return false;

            var grid = _board.Grid;
            grid.Space.TryGetCell(point, out var unclamped);
            cell = new Cell(Mathf.Clamp(unclamped.Col, 0, grid.Cols - 1), Mathf.Clamp(unclamped.Row, 0, grid.Rows - 1));
            return true;
        }

        public bool TryWorldToScreen(Vector3 world, out Vector2 screen)
        {
            var point = _camera.WorldToScreenPoint(world);
            screen = point;
            return point.z > 0f;
        }

        private void LateUpdate()
        {
            if (!_board.IsBuilt || Screen.width <= 0 || Screen.height <= 0)
                return;

            var input = new FitInput(Screen.width, Screen.height, Screen.safeArea, _insets.Top, _board.Bounds,
                _config.Tilt, _config.FieldOfView, _config.SideMargin, _config.BottomMargin, _config.TopMargin);
            if (_hasApplied && input.Equals(_applied))
                return;

            // Only the board changed: the camera has somewhere to come from, and the player is not resizing anything.
            var slide = _hasApplied && SameScreen(input, _applied) && _config.SlideSeconds > 0f;
            _applied = input;
            _hasApplied = true;
            Fit(input, slide);
        }

        private static bool SameScreen(FitInput a, FitInput b) =>
            a.Width == b.Width && a.Height == b.Height && a.SafeArea == b.SafeArea && a.TopInset.Equals(b.TopInset);

        private void Fit(FitInput input, bool slide)
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

            // The fit is the only writer of the pose: a slide still running is taken over from where it stands.
            _slide.Stop();
            if (!slide)
            {
                transform.SetPositionAndRotation(pose.Position, pose.Rotation);
                return;
            }

            transform.rotation = pose.Rotation;
            _slide = Tween.Position(transform, pose.Position, _config.SlideSeconds, _config.SlideEase, useUnscaledTime: true)
                .SetCancellationToken(destroyCancellationToken);
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
