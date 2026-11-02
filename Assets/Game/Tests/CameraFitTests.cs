using System;
using System.Collections.Generic;
using HoldIt.Board;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class CameraFitTests
    {
        private const float FieldOfView = 40f;
        private const float Tilt = 22f;

        // A 10 x 14 map on its base: 0.4 past the map on every side, walls 0.92 high, the base 0.6 deep under the floor.
        private static readonly Vector3[] _board = Corners(new Vector3(-5.4f, -0.7f, -7.4f), new Vector3(5.4f, 0.92f, 7.4f));

        private static IEnumerable<TestCaseData> Screens()
        {
            yield return new TestCaseData(1080f, 1920f, Rect.MinMaxRect(0.02f, 0.02f, 0.98f, 0.9f)).SetName("9:16");
            yield return new TestCaseData(1080f, 2340f, Rect.MinMaxRect(0.02f, 0.03f, 0.98f, 0.88f)).SetName("9:19.5 notch");
            yield return new TestCaseData(1080f, 2520f, Rect.MinMaxRect(0.02f, 0.02f, 0.98f, 0.9f)).SetName("9:21");
            yield return new TestCaseData(1536f, 2048f, Rect.MinMaxRect(0.02f, 0.02f, 0.98f, 0.9f)).SetName("3:4 tablet");
            yield return new TestCaseData(1080f, 2340f, Rect.MinMaxRect(0.08f, 0.02f, 0.98f, 0.9f)).SetName("uneven sides");
        }

        [TestCaseSource(nameof(Screens))]
        public void Fit_KeepsTheBoardInside_CentredAndTouchingOneAxis(float width, float height, Rect target)
        {
            var aspect = width / height;
            var pose = CameraFit.Fit(_board, aspect, FieldOfView, Tilt, target);

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var point in _board)
            {
                var viewport = Viewport(pose, aspect, point);
                min = Vector2.Min(min, viewport);
                max = Vector2.Max(max, viewport);
            }

            const float tolerance = 1e-3f;
            Assert.GreaterOrEqual(min.x, target.xMin - tolerance, "left");
            Assert.GreaterOrEqual(min.y, target.yMin - tolerance, "bottom");
            Assert.LessOrEqual(max.x, target.xMax + tolerance, "right");
            Assert.LessOrEqual(max.y, target.yMax + tolerance, "top");
            var fill = Mathf.Max((max.x - min.x) / target.width, (max.y - min.y) / target.height);
            Assert.AreEqual(1f, fill, tolerance, "fills one axis");
            Assert.AreEqual(target.center.x, (min.x + max.x) * 0.5f, tolerance, "centred across");
            Assert.AreEqual(target.center.y, (min.y + max.y) * 0.5f, tolerance, "centred up and down");
        }

        [Test]
        public void Fit_LooksDownAtTheTilt_TowardTheTopRow()
        {
            var pose = CameraFit.Fit(_board, 9f / 16f, FieldOfView, Tilt, new Rect(0f, 0f, 1f, 1f));
            var forward = pose.Rotation * Vector3.forward;

            Assert.AreEqual(Tilt, Vector3.Angle(forward, Vector3.down), 1e-3f);
            Assert.Greater(forward.z, 0f);
        }

        [Test]
        public void Fit_RejectsBadInput()
        {
            var full = new Rect(0f, 0f, 1f, 1f);
            Assert.Throws<ArgumentException>(() => CameraFit.Fit(new Vector3[0], 1f, FieldOfView, Tilt, full));
            Assert.Throws<ArgumentOutOfRangeException>(() => CameraFit.Fit(_board, 0f, FieldOfView, Tilt, full));
            Assert.Throws<ArgumentOutOfRangeException>(() => CameraFit.Fit(_board, 1f, 180f, Tilt, full));
            Assert.Throws<ArgumentOutOfRangeException>(() => CameraFit.Fit(_board, 1f, FieldOfView, Tilt, new Rect(0f, 0f, 0f, 1f)));
        }

        // Projects the way a Unity camera does: its view matrix looks down -Z, then the perspective projection.
        private static Vector2 Viewport(CameraPose pose, float aspect, Vector3 point)
        {
            var view = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * Matrix4x4.TRS(pose.Position, pose.Rotation, Vector3.one).inverse;
            var clip = Matrix4x4.Perspective(FieldOfView, aspect, 0.1f, 1000f) * view * new Vector4(point.x, point.y, point.z, 1f);
            return new Vector2((clip.x / clip.w + 1f) * 0.5f, (clip.y / clip.w + 1f) * 0.5f);
        }

        private static Vector3[] Corners(Vector3 min, Vector3 max)
        {
            var corners = new Vector3[8];
            for (var i = 0; i < 8; i++)
                corners[i] = new Vector3(i % 2 == 0 ? min.x : max.x, i / 2 % 2 == 0 ? min.y : max.y, i / 4 == 0 ? min.z : max.z);
            return corners;
        }
    }
}
