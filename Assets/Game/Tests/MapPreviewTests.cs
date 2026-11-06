using HoldIt.Board;
using HoldIt.UI;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class MapPreviewTests
    {
        // A different colour for every kind of tile, so a wrong one shows.
        private static readonly MapPreviewPalette _palette = new MapPreviewPalette(
            floor: new Color32(1, 0, 0, 255), lobby: new Color32(2, 0, 0, 255), entrance: new Color32(3, 0, 0, 255),
            wall: new Color32(4, 0, 0, 255), narrow: new Color32(5, 0, 0, 255), buildingMale: new Color32(6, 0, 0, 255),
            buildingFemale: new Color32(7, 0, 0, 255), doorMale: new Color32(8, 0, 0, 255), doorFemale: new Color32(9, 0, 0, 255));

        private static Color32 At(Color32[] pixels, int width, int x, int y) => pixels[y * width + x];

        [Test]
        public void Pixels_HasAPixelPerTileScaled()
        {
            var grid = MapParser.Parse(TestMaps.Small);

            Assert.AreEqual(grid.Cols * grid.Rows, MapPreview.Pixels(grid, _palette, 1).Length);
            Assert.AreEqual(grid.Cols * grid.Rows * 9, MapPreview.Pixels(grid, _palette, 3).Length);
        }

        [Test]
        public void Pixels_TheMapsTopRowIsTheTopOfTheTexture()
        {
            // Small: "MMMWWW" on top, "##EE##" at the bottom.
            var grid = MapParser.Parse(TestMaps.Small);
            var pixels = MapPreview.Pixels(grid, _palette, 1);
            var top = grid.Rows - 1;

            Assert.AreEqual(_palette.Of(new Tile(TileType.Building, Gender.Male)), At(pixels, grid.Cols, 0, top));
            Assert.AreEqual(_palette.Of(new Tile(TileType.Building, Gender.Female)), At(pixels, grid.Cols, grid.Cols - 1, top));
            Assert.AreEqual(_palette.Of(new Tile(TileType.Wall)), At(pixels, grid.Cols, 0, 0));
            Assert.AreEqual(_palette.Of(new Tile(TileType.Entrance)), At(pixels, grid.Cols, 2, 0));
        }

        [Test]
        public void Pixels_EachKindOfTileHasItsColour()
        {
            var grid = MapParser.Parse(TestMaps.Small);
            var pixels = MapPreview.Pixels(grid, _palette, 1);
            var height = grid.Rows;

            // Row 1 is "MmMWwW", row 2 "......", row 3 "LLLLLL", row 4 "##EE##" (row 0 at the top).
            Assert.AreEqual(_palette.Of(new Tile(TileType.Door, Gender.Male)), At(pixels, grid.Cols, 1, height - 1 - 1));
            Assert.AreEqual(_palette.Of(new Tile(TileType.Door, Gender.Female)), At(pixels, grid.Cols, 4, height - 1 - 1));
            Assert.AreEqual(_palette.Of(new Tile(TileType.Floor)), At(pixels, grid.Cols, 3, height - 1 - 2));
            Assert.AreEqual(_palette.Of(new Tile(TileType.Lobby)), At(pixels, grid.Cols, 3, height - 1 - 3));
        }

        [Test]
        public void Pixels_ATileFillsItsWholeSquare()
        {
            var grid = MapParser.Parse(TestMaps.Small);
            const int scale = 3;
            var pixels = MapPreview.Pixels(grid, _palette, scale);
            var width = grid.Cols * scale;
            var entrance = _palette.Of(new Tile(TileType.Entrance));

            // The first entrance of the bottom row is column 2.
            for (var y = 0; y < scale; y++)
            {
                for (var x = 2 * scale; x < 3 * scale; x++)
                    Assert.AreEqual(entrance, At(pixels, width, x, y), $"({x}, {y})");
            }
            Assert.AreNotEqual(entrance, At(pixels, width, 2 * scale - 1, 0));
            Assert.AreNotEqual(entrance, At(pixels, width, 2 * scale, scale));
        }

        [Test]
        public void Of_ANarrowPassage_IsNotTheFloor()
        {
            Assert.AreNotEqual(_palette.Of(new Tile(TileType.Floor)), _palette.Of(new Tile(TileType.Narrow)));
        }
    }
}
