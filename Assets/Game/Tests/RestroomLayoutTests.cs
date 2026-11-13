using System;
using System.Linq;
using HoldIt.Board;
using HoldIt.Restrooms;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    /// <summary>
    /// Stalls on Map 1 (<see cref="TestMaps.Lobby"/>): the men's restroom is columns 0-4 and rows 0-3 with its door at
    /// (2, 3), opening south. In world space its outer back corner is (-5, 7); "across" runs +X and depth runs -Z.
    /// </summary>
    public class RestroomLayoutTests
    {
        private static readonly StallGeometry _geometry = StallGeometry.Default;

        private ITileGrid _lobby;

        [SetUp]
        public void ParseMap() => _lobby = MapParser.Parse(TestMaps.Lobby);

        [Test]
        public void Build_Lobby_SplitsTheWidthBetweenTwoStalls()
        {
            var layout = RestroomLayout.Build(_lobby, Gender.Male, 2, _geometry);

            Assert.AreEqual(Direction.South, layout.Opening);
            Assert.AreEqual(2, layout.Stalls.Count);
            AssertNear(new Vector3(-4.2f, 0f, 5.2f), layout.Stalls[0].Hinge);
            AssertNear(new Vector3(-2.9f, 0f, 5.2f), layout.Stalls[0].Lamp);
            AssertNear(new Vector3(-3.75f, 0f, 6.86f), layout.Stalls[0].Toilet);
            AssertNear(new Vector3(-1.7f, 0f, 5.2f), layout.Stalls[1].Hinge);
            AssertNear(new Vector3(-0.4f, 0f, 5.2f), layout.Stalls[1].Lamp);
            AssertNear(new Vector3(-1.25f, 0f, 6.86f), layout.Stalls[1].Toilet);
            AssertNear(Vector3.back, layout.Stalls[0].Rotation * Vector3.forward);
        }

        [Test]
        public void Build_Lobby_WalksInAlongTheLane()
        {
            var entry = RestroomLayout.Build(_lobby, Gender.Male, 2, _geometry).Stalls[0].Entry;

            Assert.AreEqual(5, entry.Points.Count);
            AssertNear(new Vector3(-2.5f, 0f, 3.5f), entry.Points[0]);
            AssertNear(new Vector3(-2.5f, 0f, 3.9f), entry.Points[1]);
            AssertNear(new Vector3(-3.75f, 0f, 3.9f), entry.Points[2]);
            AssertNear(new Vector3(-3.75f, 0f, 5.35f), entry.Points[3]);
            AssertNear(new Vector3(-3.75f, 0f, 6.05f), entry.Points[4]);
        }

        [Test]
        public void Build_Lobby_RunsTheFrontBetweenTheDoorsAndPartsTheStalls()
        {
            var partitions = RestroomLayout.Build(_lobby, Gender.Male, 2, _geometry).Partitions;

            Assert.AreEqual(4, partitions.Count);
            AssertFront(partitions[0], new Vector3(-4.53f, 0f, 5.2f), 0.66f);
            AssertFront(partitions[1], new Vector3(-2.5f, 0f, 5.2f), 1.6f);
            AssertFront(partitions[3], new Vector3(-0.47f, 0f, 5.2f), 0.66f);

            AssertNear(new Vector3(-2.5f, 0f, 6.065f), partitions[2].Center);
            Assert.AreEqual(1.59f, partitions[2].Length, 1e-4f);
            AssertNear(Vector3.back, partitions[2].Rotation * Vector3.forward);
        }

        [Test]
        public void Build_OneStall_HasNoPartBetweenStalls()
        {
            var layout = RestroomLayout.Build(_lobby, Gender.Female, 1, _geometry);

            Assert.AreEqual(1, layout.Stalls.Count);
            Assert.AreEqual(2, layout.Partitions.Count);
            AssertNear(new Vector3(2.5f, 0f, 6.86f), layout.Stalls[0].Toilet);
        }

        // Doors off the middle, so the way in starts straight in from wherever the door is.
        private static readonly TestCaseData[] _openings =
        {
            new TestCaseData(Direction.North, new[] { ".....", "MmMMM", "MMMMM", "MMMMM", "MMMMM" }),
            new TestCaseData(Direction.East, new[] { "MMMM.", "MMMm.", "MMMM.", "MMMM.", "MMMM." }),
            new TestCaseData(Direction.South, new[] { "MMMMM", "MMMMM", "MMMMM", "MMMmM", "....." }),
            new TestCaseData(Direction.West, new[] { ".MMMM", ".MMMM", ".MMMM", ".mMMM", ".MMMM" }),
        };

        [TestCaseSource(nameof(_openings))]
        public void Build_EveryOpening_LaysTheStallsOutTheSameWay(Direction opening, string[] rows)
        {
            var grid = MapParser.Parse(rows);
            var layout = RestroomLayout.Build(grid, Gender.Male, 2, _geometry);
            var door = grid.Space.CellCenter(grid.DoorsOf(Gender.Male)[0]);
            var outward = Quaternion.Euler(0f, 90f * (int)opening, 0f) * Vector3.forward;
            var rightFromOutside = Vector3.Cross(Vector3.up, -outward);

            Assert.AreEqual(opening, layout.Opening);
            foreach (var stall in layout.Stalls)
            {
                AssertNear(outward, stall.Rotation * Vector3.forward);
                AssertNear(door, stall.Entry.Start);
                AssertNear(door - outward * (3.5f - _geometry.LaneDepth), stall.Entry.Points[1]);
                Assert.AreEqual(_geometry.InsideDepth - 3.5f, Vector3.Dot(stall.Entry.End - door, outward), 1e-4f);
                Assert.AreEqual(_geometry.WallThickness - 3.5f, Vector3.Dot(stall.Toilet - door, outward), 1e-4f);
                Assert.AreEqual(_geometry.StallDepth - 3.5f, Vector3.Dot(stall.Hinge - door, outward), 1e-4f);
            }
            Assert.AreEqual(2.5f, Vector3.Dot(layout.Stalls[1].Toilet - layout.Stalls[0].Toilet, rightFromOutside), 1e-4f,
                "Stall 0 is on the left seen from outside.");
            Assert.Greater(Vector3.Dot(layout.Stalls[0].Lamp - layout.Stalls[0].Hinge, rightFromOutside), 0f,
                "The lamp is right of the door.");
        }

        [Test]
        public void Problems_Lobby_HasNone()
        {
            CollectionAssert.IsEmpty(RestroomLayout.Problems(_lobby, Gender.Male, 2, _geometry));
            CollectionAssert.IsEmpty(RestroomLayout.Problems(_lobby, Gender.Female, 3, _geometry));
        }

        [Test]
        public void Problems_TooManyStalls_SaysHowWideTheyNeed()
        {
            var problems = RestroomLayout.Problems(_lobby, Gender.Male, 4, _geometry);

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("5 tiles across: 4 stalls need 6", problems[0]);
        }

        [Test]
        public void Problems_NoStall_IsReported()
        {
            var problems = RestroomLayout.Problems(_lobby, Gender.Male, 0, _geometry);

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("at least one stall", problems[0]);
        }

        [Test]
        public void Problems_TooShallow_SaysHowDeepTheWayInNeeds()
        {
            var problems = RestroomLayout.Problems(MapParser.Parse(TestMaps.Small), Gender.Female, 2, _geometry);

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("2 tiles deep from its door: the way into its stalls needs 3.6", problems[0]);
        }

        [Test]
        public void Problems_WithoutADoor_SaysSoAndNothingElse()
        {
            var grid = MapParser.Parse(new[] { "MMMWWW", "MMMWwW", "......", "LLLLLL", "##EE##" });

            var problems = RestroomLayout.Problems(grid, Gender.Male, 0, _geometry);

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("one door", problems[0]);
        }

        [Test]
        public void Build_WithProblems_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                RestroomLayout.Build(MapParser.Parse(TestMaps.Small), Gender.Male, 2, _geometry));
            Assert.Throws<ArgumentException>(() => RestroomLayout.Build(_lobby, Gender.Male, 4, _geometry));
        }

        [Test]
        public void Build_EveryWayIn_StaysInsideTheRestroom()
        {
            var layout = RestroomLayout.Build(_lobby, Gender.Male, 3, _geometry);

            foreach (var point in layout.Stalls.SelectMany(stall => stall.Entry.Points))
            {
                Assert.IsTrue(point.x > -5f && point.x < 0f && point.z > 3f && point.z < 7f, $"{point} is outside.");
            }
        }

        // An open stall door swings out as far as it is long, so the sideways walk to a stall must stay beyond that.
        [Test]
        public void Build_EveryWayIn_WalksSidewaysBeyondTheReachOfAnOpenDoor()
        {
            var layout = RestroomLayout.Build(_lobby, Gender.Male, 3, _geometry);
            var reach = _geometry.StallDepth + _geometry.DoorWidth;

            foreach (var stall in layout.Stalls)
            {
                var points = stall.Entry.Points;
                for (var i = 0; i < points.Count - 1; i++)
                {
                    if (Mathf.Approximately(points[i].x, points[i + 1].x))
                        continue;
                    var depthA = 7f - points[i].z;
                    var depthB = 7f - points[i + 1].z;
                    Assert.Greater(Mathf.Min(depthA, depthB), reach, $"The walk {points[i]} -> {points[i + 1]} cuts through an open door.");
                }
            }
        }

        // The same walk for every way the restroom can open: it goes inward to the lane, along it, and only then in.
        [TestCaseSource(nameof(_openings))]
        public void Build_EveryOpening_WalksSidewaysOnlyAlongTheLane(Direction opening, string[] rows)
        {
            var grid = MapParser.Parse(rows);
            var layout = RestroomLayout.Build(grid, Gender.Male, 2, _geometry);
            var door = grid.Space.CellCenter(grid.DoorsOf(Gender.Male)[0]);
            var outward = Quaternion.Euler(0f, 90f * (int)opening, 0f) * Vector3.forward;
            var sideways = 0;

            foreach (var stall in layout.Stalls)
            {
                var points = stall.Entry.Points;
                for (var i = 0; i < points.Count - 1; i++)
                {
                    var along = Vector3.Dot(points[i + 1] - points[i], outward);
                    var across = (points[i + 1] - points[i] - outward * along).magnitude;
                    if (across < 1e-4f)
                        continue;
                    sideways++;
                    Assert.AreEqual(0f, along, 1e-4f, "A sideways step must not also go in or out.");
                    var depth = 3.5f + Vector3.Dot(points[i] - door, outward);
                    Assert.AreEqual(_geometry.LaneDepth, depth, 1e-4f, $"The sideways step {points[i]} is off the lane.");
                }
            }
            Assert.Greater(sideways, 0, "These doors are off the middle, so some stall needs a step along the lane.");
        }

        // A lane exactly as deep as the door leaves nothing to walk in to; the way in starts along the lane.
        [Test]
        public void Build_ALaneAsDeepAsTheDoor_SkipsTheStepIn()
        {
            var geometry = new StallGeometry(1.8f, 0.9f, 0.14f, 0.3f, 0.8f, 0.15f, 0.95f);
            Assert.AreEqual(3.5f, geometry.LaneDepth, 1e-5f);

            var entry = RestroomLayout.Build(_lobby, Gender.Male, 2, geometry).Stalls[0].Entry;

            Assert.AreEqual(4, entry.Points.Count);
            AssertNear(new Vector3(-2.5f, 0f, 3.5f), entry.Points[0]);
            AssertNear(new Vector3(-3.75f, 0f, 3.5f), entry.Points[1]);
        }

        private static void AssertFront(PartitionPlan partition, Vector3 center, float length)
        {
            AssertNear(center, partition.Center);
            Assert.AreEqual(length, partition.Length, 1e-4f);
            AssertNear(Vector3.right, partition.Rotation * Vector3.forward);
        }

        private static void AssertNear(Vector3 expected, Vector3 actual) =>
            Assert.Less(Vector3.Distance(expected, actual), 1e-4f, $"Expected {expected}, got {actual}.");
    }
}
