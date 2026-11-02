using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>
    /// Turns a map into the pieces that show it. Each tile gets its floor (a wall tile its block, a door tile its doorway);
    /// a narrow tile also gets posts across its passage; a restroom gets an outer wall on every edge of its rectangle except
    /// the one its door opens to. Assumes a map that passes <see cref="MapCheck"/>.
    /// </summary>
    public static class BoardLayout
    {
        public static List<PiecePlacement> Build(ITileGrid grid)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));

            var pieces = new List<PiecePlacement>();
            for (var row = 0; row < grid.Rows; row++)
            {
                for (var col = 0; col < grid.Cols; col++)
                    AddTile(grid, new Cell(col, row), pieces);
            }
            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
                AddBuildingWalls(grid, gender, pieces);
            return pieces;
        }

        private static void AddTile(ITileGrid grid, Cell cell, List<PiecePlacement> pieces)
        {
            var tile = grid.At(cell);
            switch (tile.Type)
            {
                case TileType.Floor:
                    pieces.Add(new PiecePlacement(PieceKind.Floor, cell));
                    break;
                case TileType.Lobby:
                    pieces.Add(new PiecePlacement(PieceKind.LobbyFloor, cell));
                    break;
                case TileType.Entrance:
                    pieces.Add(new PiecePlacement(PieceKind.EntranceFloor, cell));
                    break;
                case TileType.Wall:
                    pieces.Add(new PiecePlacement(PieceKind.WallBlock, cell));
                    break;
                case TileType.Narrow:
                    pieces.Add(new PiecePlacement(PieceKind.Floor, cell));
                    pieces.Add(new PiecePlacement(PieceKind.NarrowPosts, cell, PassageFacing(grid, cell)));
                    break;
                case TileType.Building:
                    pieces.Add(new PiecePlacement(PieceKind.BuildingFloor, cell, gender: tile.Gender));
                    break;
                case TileType.Door:
                    pieces.Add(new PiecePlacement(PieceKind.Doorway, cell, Opening(grid, cell, tile.Gender), tile.Gender));
                    break;
            }
        }

        private static void AddBuildingWalls(ITileGrid grid, Gender gender, List<PiecePlacement> pieces)
        {
            var building = grid.BuildingOf(gender);
            if (building.width == 0)
                return;

            var doors = grid.DoorsOf(gender);
            var door = doors.Count > 0 ? doors[0] : new Cell(-1, -1);
            var opening = doors.Count > 0 ? Opening(grid, door, gender) : Direction.South;
            foreach (var position in building.allPositionsWithin)
            {
                var cell = new Cell(position.x, position.y);
                foreach (var direction in Directions.All)
                {
                    var next = cell.Step(direction);
                    if (building.Contains(new Vector2Int(next.Col, next.Row)))
                        continue;
                    if (cell == door && direction == opening)
                        continue;
                    pieces.Add(new PiecePlacement(PieceKind.BuildingWall, cell, direction, gender));
                }
            }
        }

        // The posts stand across the passage: east and west of it (facing North) unless both its north and south sides are
        // closed, which makes it an east-west passage (facing East turns the posts to the north and south edges).
        private static Direction PassageFacing(ITileGrid grid, Cell cell) =>
            !grid.IsWalkable(cell.Step(Direction.North)) && !grid.IsWalkable(cell.Step(Direction.South))
                ? Direction.East
                : Direction.North;

        private static Direction Opening(ITileGrid grid, Cell door, Gender? gender)
        {
            if (!gender.HasValue)
                return Direction.South;
            var openings = Doorway.Openings(grid, door, grid.BuildingOf(gender.Value));
            return openings.Count > 0 ? openings[0] : Direction.South;
        }
    }
}
