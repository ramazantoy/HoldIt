using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>Which way a restroom's door opens.</summary>
    public static class Doorway
    {
        /// <summary>
        /// The sides of the door that lead to a walkable tile outside its building. A valid map has exactly one
        /// (<see cref="MapCheck"/>); it is where the door frame stands and where characters come in.
        /// </summary>
        public static List<Direction> Openings(ITileGrid grid, Cell door, RectInt building)
        {
            var openings = new List<Direction>();
            foreach (var direction in Directions.All)
            {
                var next = door.Step(direction);
                if (!building.Contains(new Vector2Int(next.Col, next.Row)) && grid.IsWalkable(next))
                    openings.Add(direction);
            }
            return openings;
        }
    }
}
