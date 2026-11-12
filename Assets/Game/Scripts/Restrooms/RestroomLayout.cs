using System;
using System.Collections.Generic;
using System.Globalization;
using HoldIt.Board;
using HoldIt.Paths;
using UnityEngine;

namespace HoldIt.Restrooms
{
    /// <summary>
    /// Where a restroom's stalls stand, in world space. They line the back wall, the one across from the door, and share
    /// its width evenly; a front partition runs across them with a door opening in the middle of each stall, and
    /// partitions part neighbouring stalls. A visitor walks in from the door's centre to the lane in front of the open stall
    /// doors, along it to its stall and through the opening. The way the door opens sets the restroom's own axes, so any of the four
    /// works. Assumes a map that passes <see cref="MapCheck"/>.
    /// </summary>
    public sealed class RestroomLayout
    {
        private const float HalfTile = 0.5f;

        private RestroomLayout(Gender gender, Direction opening, StallPlan[] stalls, PartitionPlan[] partitions)
        {
            Gender = gender;
            Opening = opening;
            Stalls = stalls;
            Partitions = partitions;
        }

        public Gender Gender { get; }

        /// <summary>The side the restroom's door opens to; its stalls face that way.</summary>
        public Direction Opening { get; }

        /// <summary>In order along the restroom: from the left as seen through its door from outside.</summary>
        public IReadOnlyList<StallPlan> Stalls { get; }

        public IReadOnlyList<PartitionPlan> Partitions { get; }

        /// <summary>Lays out <paramref name="stallCount"/> stalls. Throws for a restroom with <see cref="Problems"/>.</summary>
        public static RestroomLayout Build(ITileGrid grid, Gender gender, int stallCount, StallGeometry geometry)
        {
            var problems = Problems(grid, gender, stallCount, geometry);
            if (problems.Count > 0)
                throw new ArgumentException(string.Join(" ", problems), nameof(grid));

            var frame = Frame.Of(grid, gender);
            var width = frame.Width / stallCount;
            var front = geometry.StallDepth;
            var halfDoor = geometry.DoorWidth * 0.5f;
            var wall = geometry.WallThickness;
            var rotation = frame.Opening.Rotation();

            var stalls = new StallPlan[stallCount];
            var partitions = new List<PartitionPlan>();
            var segmentStart = wall;
            for (var i = 0; i < stallCount; i++)
            {
                var left = i * width;
                var right = left + width;
                var middle = left + width * 0.5f;

                var entry = new WalkLine(new[]
                {
                    frame.Door,
                    frame.At(frame.DoorAcross, geometry.LaneDepth),
                    frame.At(middle, geometry.LaneDepth),
                    frame.At(middle, front - geometry.Pass),
                    frame.At(middle, geometry.InsideDepth),
                });
                stalls[i] = new StallPlan(frame.At(middle - halfDoor, front),
                    frame.At((middle + halfDoor + right) * 0.5f, front), frame.At(middle, wall), rotation, entry);

                partitions.Add(Across(frame, segmentStart, middle - halfDoor, front));
                segmentStart = middle + halfDoor;
                if (i > 0)
                    partitions.Add(Deep(frame, left, wall, front - wall * 0.5f));
            }
            partitions.Add(Across(frame, segmentStart, frame.Width - wall, front));
            return new RestroomLayout(gender, frame.Opening, stalls, partitions.ToArray());
        }

        /// <summary>
        /// Why the stalls do not fit, in a fixed order; empty when they do. A restroom that does not pass
        /// <see cref="MapCheck"/> is reported once, without looking further.
        /// </summary>
        public static IReadOnlyList<string> Problems(ITileGrid grid, Gender gender, int stallCount, StallGeometry geometry)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));

            var problems = new List<string>();
            if (!Frame.TryOf(grid, gender, out var frame))
            {
                problems.Add($"The {Name(gender)} restroom needs a building with one door that opens outside.");
                return problems;
            }

            if (stallCount < 1)
            {
                problems.Add($"The {Name(gender)} restroom needs at least one stall, not {stallCount}.");
                return problems;
            }

            var needed = MinWidth(stallCount, geometry);
            if (frame.Width + 1e-4f < needed)
                problems.Add($"The {Name(gender)} restroom is {Number(frame.Width)} tiles across: {stallCount} " +
                             $"stalls need {Number(needed)}.");

            var deepest = MinDepth(geometry);
            if (frame.Depth + 1e-4f < deepest)
                problems.Add($"The {Name(gender)} restroom is {Number(frame.Depth)} tiles deep from its door: the way " +
                             $"into its stalls needs {Number(deepest)}.");
            return problems;
        }

        /// <summary>How many tiles across a restroom must be, along its back wall, to hold that many stalls.</summary>
        public static float MinWidth(int stallCount, StallGeometry geometry) => stallCount * geometry.MinStallWidth;

        /// <summary>How many tiles deep a restroom must be, from its door to its back wall's outer face, door tile included.</summary>
        public static float MinDepth(StallGeometry geometry) =>
            geometry.LaneDepth + HalfTile;

        private static PartitionPlan Across(Frame frame, float from, float to, float depth) =>
            new PartitionPlan(frame.At((from + to) * 0.5f, depth), Quaternion.LookRotation(frame.Across), to - from);

        private static PartitionPlan Deep(Frame frame, float across, float from, float to) =>
            new PartitionPlan(frame.At(across, (from + to) * 0.5f), Quaternion.LookRotation(frame.Out), to - from);

        private static string Name(Gender gender) => gender == Gender.Male ? "men's" : "women's";

        private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>
        /// The restroom's own axes. "Across" runs along the back wall, left to right as seen through the door from
        /// outside; "depth" runs from the back wall's outer face toward the door. Both are in tiles.
        /// </summary>
        private readonly struct Frame
        {
            public readonly Direction Opening;
            public readonly Vector3 Across;
            public readonly Vector3 Out;
            public readonly float Width;
            public readonly float Depth;
            public readonly Vector3 Door;
            public readonly float DoorAcross;
            public readonly float DoorDepth;
            private readonly Vector3 _origin;

            private Frame(Direction opening, Vector3 origin, float width, float depth, Vector3 door)
            {
                Opening = opening;
                Out = Vector(opening);
                Across = Vector(opening.Clockwise().Opposite());
                Width = width;
                Depth = depth;
                Door = door;
                _origin = origin;
                DoorAcross = Vector3.Dot(door - origin, Across);
                DoorDepth = Vector3.Dot(door - origin, Out);
            }

            public Vector3 At(float across, float depth) => _origin + Across * across + Out * depth;

            public static Frame Of(ITileGrid grid, Gender gender)
            {
                if (!TryOf(grid, gender, out var frame))
                    throw new ArgumentException($"The {Name(gender)} restroom has no single door that opens outside.");
                return frame;
            }

            public static bool TryOf(ITileGrid grid, Gender gender, out Frame frame)
            {
                frame = default;
                var doors = grid.DoorsOf(gender);
                var building = grid.BuildingOf(gender);
                if (doors.Count != 1 || building.width == 0)
                    return false;
                var openings = Doorway.Openings(grid, doors[0], building);
                if (openings.Count != 1)
                    return false;

                var opening = openings[0];
                var space = grid.Space;
                var center = (space.CellCenter(new Cell(building.xMin, building.yMin)) +
                              space.CellCenter(new Cell(building.xMax - 1, building.yMax - 1))) * 0.5f;
                var sideways = opening == Direction.North || opening == Direction.South;
                float width = sideways ? building.width : building.height;
                float depth = sideways ? building.height : building.width;
                var origin = center - Vector(opening.Clockwise().Opposite()) * (width * 0.5f) -
                             Vector(opening) * (depth * 0.5f);
                frame = new Frame(opening, origin, width, depth, space.CellCenter(doors[0]));
                return true;
            }

            private static Vector3 Vector(Direction direction) => direction switch
            {
                Direction.North => Vector3.forward,
                Direction.East => Vector3.right,
                Direction.South => Vector3.back,
                _ => Vector3.left,
            };
        }
    }
}
