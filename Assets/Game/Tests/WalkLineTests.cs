using System;
using HoldIt.Paths;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class WalkLineTests
    {
        /// <summary>3 east, then 4 north: 7 long, with a corner at 3.</summary>
        private static readonly WalkLine _corner =
            new WalkLine(new[] { Vector3.zero, new Vector3(3f, 0f, 0f), new Vector3(3f, 0f, 4f) });

        [Test]
        public void Length_AddsEverySegment()
        {
            Assert.AreEqual(7f, _corner.Length, 1e-5f);
            Assert.AreEqual(Vector3.zero, _corner.Start);
            Assert.AreEqual(new Vector3(3f, 0f, 4f), _corner.End);
        }

        [Test]
        public void PointAt_FollowsTheSegments()
        {
            AssertNear(new Vector3(1.5f, 0f, 0f), _corner.PointAt(1.5f));
            AssertNear(new Vector3(3f, 0f, 0f), _corner.PointAt(3f));
            AssertNear(new Vector3(3f, 0f, 2f), _corner.PointAt(5f));
        }

        [Test]
        public void PointAt_OutsideTheLine_KeepsToItsEnds()
        {
            AssertNear(_corner.Start, _corner.PointAt(-1f));
            AssertNear(_corner.End, _corner.PointAt(100f));
        }

        [Test]
        public void DirectionAt_IsTheSegmentsDirection()
        {
            AssertNear(Vector3.right, _corner.DirectionAt(1f));
            AssertNear(Vector3.forward, _corner.DirectionAt(4f));
            AssertNear(Vector3.forward, _corner.DirectionAt(100f));
        }

        [Test]
        public void New_DropsRepeatedPoints()
        {
            var line = new WalkLine(new[] { Vector3.zero, Vector3.zero, Vector3.right, Vector3.right });

            Assert.AreEqual(2, line.Points.Count);
            Assert.AreEqual(1f, line.Length, 1e-5f);
            AssertNear(Vector3.right, line.DirectionAt(0f));
        }

        [Test]
        public void New_WithoutTwoDifferentPoints_Throws()
        {
            Assert.Throws<ArgumentException>(() => new WalkLine(new[] { Vector3.one }));
            Assert.Throws<ArgumentException>(() => new WalkLine(new[] { Vector3.one, Vector3.one }));
            Assert.Throws<ArgumentNullException>(() => new WalkLine(null));
        }

        private static void AssertNear(Vector3 expected, Vector3 actual) =>
            Assert.Less(Vector3.Distance(expected, actual), 1e-4f, $"Expected {expected}, got {actual}.");
    }
}
