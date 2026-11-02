using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>
    /// Where the map's cells are in the world. One tile is one unit, the board's centre is the world origin and its floor is
    /// the y = 0 plane. Rows run toward -Z, so row 0 is the farthest from the camera.
    /// </summary>
    public readonly struct BoardSpace
    {
        public readonly int Cols;
        public readonly int Rows;

        public BoardSpace(int cols, int rows)
        {
            Cols = cols;
            Rows = rows;
        }

        /// <summary>The centre of the cell on the floor.</summary>
        public Vector3 CellCenter(Cell cell) =>
            new Vector3(cell.Col - (Cols - 1) * 0.5f, 0f, (Rows - 1) * 0.5f - cell.Row);

        /// <summary>The cell under a world point (its height is ignored). False outside the map.</summary>
        public bool TryGetCell(Vector3 world, out Cell cell)
        {
            cell = new Cell(Mathf.FloorToInt(world.x + Cols * 0.5f), Mathf.FloorToInt(Rows * 0.5f - world.z));
            return cell.Col >= 0 && cell.Col < Cols && cell.Row >= 0 && cell.Row < Rows;
        }

        /// <summary>Where a ray meets the floor plane. False if it runs parallel to it or away from it.</summary>
        public static bool TryHitFloor(Ray ray, out Vector3 point)
        {
            point = default;
            if (Mathf.Abs(ray.direction.y) < 1e-6f)
                return false;

            var distance = -ray.origin.y / ray.direction.y;
            if (distance < 0f)
                return false;

            point = ray.origin + ray.direction * distance;
            return true;
        }
    }
}
