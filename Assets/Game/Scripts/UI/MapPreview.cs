using HoldIt.Board;
using UnityEngine;

namespace HoldIt.UI
{
    /// <summary>
    /// A level's map as a small picture for the level select: every tile a square of one colour (<see cref="MapPreviewPalette"/>),
    /// the map's top row at the top as the board shows it. Pure; the view puts the pixels in a texture.
    /// </summary>
    public static class MapPreview
    {
        /// <summary>Pixels for a texture of <c>Cols * pixelsPerTile</c> by <c>Rows * pixelsPerTile</c>, row by row from the bottom.</summary>
        public static Color32[] Pixels(ITileGrid grid, MapPreviewPalette palette, int pixelsPerTile)
        {
            var width = grid.Cols * pixelsPerTile;
            var height = grid.Rows * pixelsPerTile;
            var pixels = new Color32[width * height];
            for (var row = 0; row < grid.Rows; row++)
            {
                // The map's first row is its top; a texture's first row is its bottom.
                var top = (grid.Rows - 1 - row) * pixelsPerTile;
                for (var col = 0; col < grid.Cols; col++)
                {
                    var color = palette.Of(grid.At(new Cell(col, row)));
                    for (var y = 0; y < pixelsPerTile; y++)
                    {
                        var start = (top + y) * width + col * pixelsPerTile;
                        for (var x = 0; x < pixelsPerTile; x++)
                            pixels[start + x] = color;
                    }
                }
            }
            return pixels;
        }
    }
}
