using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>
    /// Places a tilted perspective camera so a set of world points fills a rectangle of the screen. The camera looks toward
    /// +Z, tilted from straight down by a fixed angle; only its position changes. The points end up inside the rectangle,
    /// centred in it and touching it on one axis.
    /// </summary>
    public static class CameraFit
    {
        private const int MaxIterations = 100;
        private const float Tolerance = 1e-5f;

        /// <param name="points">World points that must be in view, such as the corners of the board's bounds.</param>
        /// <param name="aspect">Screen width over height.</param>
        /// <param name="fieldOfView">Vertical field of view in degrees, as <c>Camera.fieldOfView</c>.</param>
        /// <param name="tilt">Degrees away from looking straight down.</param>
        /// <param name="target">The screen rectangle to fill, in viewport coordinates (0 to 1, origin bottom left).</param>
        public static CameraPose Fit(IReadOnlyList<Vector3> points, float aspect, float fieldOfView, float tilt, Rect target)
        {
            if (points == null || points.Count == 0)
                throw new ArgumentException("There is nothing to fit.", nameof(points));
            if (aspect <= 0f)
                throw new ArgumentOutOfRangeException(nameof(aspect), aspect, "The aspect must be positive.");
            if (fieldOfView <= 0f || fieldOfView >= 180f)
                throw new ArgumentOutOfRangeException(nameof(fieldOfView), fieldOfView, "The field of view must be in (0, 180).");
            if (target.width <= 0f || target.height <= 0f)
                throw new ArgumentOutOfRangeException(nameof(target), target, "The target rectangle is empty.");

            var rotation = Quaternion.Euler(90f - tilt, 0f, 0f);
            var inverse = Quaternion.Inverse(rotation);
            var back = rotation * Vector3.back;
            var tanHalf = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            var cosTilt = Mathf.Max(Mathf.Cos(tilt * Mathf.Deg2Rad), 0.01f);

            var pivot = Center(points, out var extent);
            var distance = Mathf.Max(extent, 0.01f) / Mathf.Min(tanHalf, tanHalf * aspect) * 2f;

            for (var i = 0; i < MaxIterations; i++)
            {
                var position = pivot + back * distance;
                if (!TryMeasure(points, position, inverse, tanHalf, aspect, out var min, out var max))
                {
                    distance *= 2f;
                    continue;
                }

                var scale = Mathf.Max((max.x - min.x) / target.width, (max.y - min.y) / target.height);
                var shift = (min + max) * 0.5f - target.center;
                if (Mathf.Abs(scale - 1f) < Tolerance && Mathf.Abs(shift.x) < Tolerance && Mathf.Abs(shift.y) < Tolerance)
                    break;

                // A viewport shift of d is 2d in clip space; at this distance that is a world move of 2d * distance * tan.
                pivot.x += 2f * shift.x * distance * tanHalf * aspect;
                pivot.z += 2f * shift.y * distance * tanHalf / cosTilt;
                distance *= Mathf.Clamp(scale, 0.5f, 2f);
            }

            return new CameraPose(pivot + back * distance, rotation);
        }

        private static Vector3 Center(IReadOnlyList<Vector3> points, out float extent)
        {
            var sum = Vector3.zero;
            foreach (var point in points)
                sum += point;
            var center = sum / points.Count;

            extent = 0f;
            foreach (var point in points)
                extent = Mathf.Max(extent, (point - center).magnitude);
            return center;
        }

        // The points' viewport bounds seen from the position; false if one is not in front of the camera.
        private static bool TryMeasure(IReadOnlyList<Vector3> points, Vector3 position, Quaternion inverse, float tanHalf,
            float aspect, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            foreach (var point in points)
            {
                var local = inverse * (point - position);
                if (local.z <= 1e-4f)
                    return false;

                var viewport = new Vector2(
                    (local.x / (local.z * tanHalf * aspect) + 1f) * 0.5f,
                    (local.y / (local.z * tanHalf) + 1f) * 0.5f);
                min = Vector2.Min(min, viewport);
                max = Vector2.Max(max, viewport);
            }
            return true;
        }
    }
}
