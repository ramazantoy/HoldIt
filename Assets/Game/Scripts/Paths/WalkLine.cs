using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Paths
{
    /// <summary>
    /// A walk through world points, for a way that does not follow the grid (into a stall). Immutable. Points that repeat
    /// the one before are dropped, so every segment has a length and a direction.
    /// </summary>
    public sealed class WalkLine
    {
        private const float SamePointEpsilon = 1e-4f;

        private readonly Vector3[] _points;
        private readonly float[] _starts;

        /// <summary>Throws unless at least two different points remain.</summary>
        public WalkLine(IEnumerable<Vector3> points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            var kept = new List<Vector3>();
            foreach (var point in points)
            {
                if (kept.Count == 0 || Vector3.Distance(kept[kept.Count - 1], point) > SamePointEpsilon)
                    kept.Add(point);
            }
            if (kept.Count < 2)
                throw new ArgumentException("A walk needs at least two different points.", nameof(points));

            _points = kept.ToArray();
            _starts = new float[_points.Length];
            for (var i = 1; i < _points.Length; i++)
                _starts[i] = _starts[i - 1] + Vector3.Distance(_points[i - 1], _points[i]);
        }

        public IReadOnlyList<Vector3> Points => _points;
        public Vector3 Start => _points[0];
        public Vector3 End => _points[_points.Length - 1];

        /// <summary>The distance along every segment, in world units.</summary>
        public float Length => _starts[_starts.Length - 1];

        /// <summary>Where the walk is after <paramref name="distance"/>, kept between its start and its end.</summary>
        public Vector3 PointAt(float distance)
        {
            var segment = SegmentAt(distance);
            var length = _starts[segment + 1] - _starts[segment];
            var t = Mathf.Clamp01((distance - _starts[segment]) / length);
            return Vector3.Lerp(_points[segment], _points[segment + 1], t);
        }

        /// <summary>The unit direction of the segment walked at <paramref name="distance"/>; at the end, the last one's.</summary>
        public Vector3 DirectionAt(float distance)
        {
            var segment = SegmentAt(distance);
            return (_points[segment + 1] - _points[segment]).normalized;
        }

        private int SegmentAt(float distance)
        {
            var last = _points.Length - 2;
            for (var i = 0; i < last; i++)
            {
                if (distance < _starts[i + 1])
                    return i;
            }
            return last;
        }
    }
}
