using System;
using System.Collections.Generic;

namespace HoldIt.Board
{
    /// <summary>
    /// Whether a parsed map can be played. A map needs an entrance, and for each gender one restroom: its building tiles
    /// fill a rectangle together with a single door on the rectangle's edge, and that door opens onto exactly one walkable
    /// tile outside. The two doors are not side by side. From the entrances a lobby tile and both doors must be reachable,
    /// also for Fat characters, who cannot pass narrow tiles. Walking through a door is not allowed on the way.
    /// </summary>
    public static class MapCheck
    {
        /// <summary>Every problem found, in a fixed order; empty for a playable map.</summary>
        public static IReadOnlyList<string> Problems(ITileGrid grid)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));

            var problems = new List<string>();
            if (grid.Entrances.Count == 0)
                problems.Add("The map has no entrance (E).");
            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
                CheckRestroom(grid, gender, problems);
            CheckDoorsApart(grid, problems);
            CheckReach(grid, problems);
            return problems;
        }

        // Side by side, the two door frames would share their posts.
        private static void CheckDoorsApart(ITileGrid grid, List<string> problems)
        {
            var male = grid.DoorsOf(Gender.Male);
            var female = grid.DoorsOf(Gender.Female);
            if (male.Count != 1 || female.Count != 1)
                return;

            foreach (var direction in Directions.All)
            {
                if (male[0].Step(direction) == female[0])
                {
                    problems.Add($"The men's door at {male[0]} and the women's door at {female[0]} are side by side.");
                    return;
                }
            }
        }

        private static void CheckRestroom(ITileGrid grid, Gender gender, List<string> problems)
        {
            var doors = grid.DoorsOf(gender);
            if (doors.Count == 0)
            {
                problems.Add($"There is no {Name(gender)} door ({DoorSymbol(gender)}).");
                return;
            }
            if (doors.Count > 1)
            {
                problems.Add($"There are {doors.Count} {Name(gender)} doors ({DoorSymbol(gender)}); a map has one.");
                return;
            }

            var door = doors[0];
            var building = grid.BuildingOf(gender);
            var buildingTile = new Tile(TileType.Building, gender);
            var hasBuilding = false;
            foreach (var position in building.allPositionsWithin)
            {
                var cell = new Cell(position.x, position.y);
                if (cell == door)
                    continue;
                if (grid.At(cell) != buildingTile)
                {
                    problems.Add($"The {Name(gender)} building ({BuildingSymbol(gender)}) is not a filled rectangle " +
                                 $"with its door: {cell} is {grid.At(cell)}.");
                    return;
                }
                hasBuilding = true;
            }
            if (!hasBuilding)
            {
                problems.Add($"There is no {Name(gender)} building ({BuildingSymbol(gender)}).");
                return;
            }

            var openings = Doorway.Openings(grid, door, building).Count;
            if (openings != 1)
                problems.Add($"The {Name(gender)} door ({DoorSymbol(gender)}) at {door} must open onto exactly one " +
                             $"walkable tile outside its building; it opens onto {openings}.");
        }

        private static void CheckReach(ITileGrid grid, List<string> problems)
        {
            if (grid.Entrances.Count == 0)
                return;

            var anyone = Reachable(grid, narrowAllowed: true);
            var fat = Reachable(grid, narrowAllowed: false);
            if (!ContainsLobby(grid, anyone))
                problems.Add("No lobby tile (L) can be reached from an entrance.");
            else if (!ContainsLobby(grid, fat))
                problems.Add("Fat characters cannot reach a lobby tile (L): every way goes through a narrow passage (n).");

            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
            {
                var doors = grid.DoorsOf(gender);
                if (doors.Count != 1)
                    continue;
                if (!anyone.Contains(doors[0]))
                    problems.Add($"The {Name(gender)} door ({DoorSymbol(gender)}) cannot be reached from an entrance.");
                else if (!fat.Contains(doors[0]))
                    problems.Add($"Fat characters cannot reach the {Name(gender)} door ({DoorSymbol(gender)}): every way " +
                                 "goes through a narrow passage (n).");
            }
        }

        // Breadth-first from every entrance over walkable tiles. Doors are reached but never walked through.
        private static HashSet<Cell> Reachable(ITileGrid grid, bool narrowAllowed)
        {
            var reached = new HashSet<Cell>();
            var queue = new Queue<Cell>();
            foreach (var entrance in grid.Entrances)
            {
                if (reached.Add(entrance))
                    queue.Enqueue(entrance);
            }

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (grid.At(cell).Type == TileType.Door)
                    continue;

                foreach (var direction in Directions.All)
                {
                    var next = cell.Step(direction);
                    if (!grid.IsWalkable(next) || reached.Contains(next) || !Passage.CanStep(grid, cell, next))
                        continue;
                    if (!narrowAllowed && grid.At(next).Type == TileType.Narrow)
                        continue;
                    reached.Add(next);
                    queue.Enqueue(next);
                }
            }
            return reached;
        }

        private static bool ContainsLobby(ITileGrid grid, HashSet<Cell> cells)
        {
            foreach (var cell in cells)
            {
                if (grid.At(cell).Type == TileType.Lobby)
                    return true;
            }
            return false;
        }

        private static string Name(Gender gender) => gender == Gender.Male ? "men's" : "women's";
        private static string DoorSymbol(Gender gender) => gender == Gender.Male ? "m" : "w";
        private static string BuildingSymbol(Gender gender) => gender == Gender.Male ? "M" : "W";
    }
}
