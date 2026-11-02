using HoldIt.Board;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class MapParserTests
    {
        [Test]
        public void Parse_ReadsTheSizeAndEveryKindOfTile()
        {
            var grid = MapParser.Parse(TestMaps.Lobby);

            Assert.AreEqual(10, grid.Cols);
            Assert.AreEqual(14, grid.Rows);
            Assert.AreEqual(new Tile(TileType.Building, Gender.Male), grid.At(new Cell(0, 0)));
            Assert.AreEqual(new Tile(TileType.Building, Gender.Female), grid.At(new Cell(9, 0)));
            Assert.AreEqual(new Tile(TileType.Door, Gender.Male), grid.At(new Cell(2, 3)));
            Assert.AreEqual(new Tile(TileType.Door, Gender.Female), grid.At(new Cell(7, 3)));
            Assert.AreEqual(new Tile(TileType.Floor), grid.At(new Cell(0, 4)));
            Assert.AreEqual(new Tile(TileType.Wall), grid.At(new Cell(1, 5)));
            Assert.AreEqual(new Tile(TileType.Narrow), grid.At(new Cell(4, 8)));
            Assert.AreEqual(new Tile(TileType.Lobby), grid.At(new Cell(1, 12)));
            Assert.AreEqual(new Tile(TileType.Entrance), grid.At(new Cell(5, 13)));
        }

        [Test]
        public void Parse_UnevenRow_ThrowsWithTheRow()
        {
            var exception = Assert.Throws<MapFormatException>(() => MapParser.Parse(new[] { "...", "..", "..." }));

            StringAssert.Contains("Row 1 has 2 tiles; row 0 has 3", exception.Message);
        }

        [Test]
        public void Parse_UnknownCharacter_ThrowsWithItsPlace()
        {
            var exception = Assert.Throws<MapFormatException>(() => MapParser.Parse(new[] { "...", ".x." }));

            StringAssert.Contains("'x' at column 1, row 1", exception.Message);
        }

        [Test]
        public void Parse_NoRows_Throws()
        {
            Assert.Throws<MapFormatException>(() => MapParser.Parse(null));
            Assert.Throws<MapFormatException>(() => MapParser.Parse(new string[0]));
            Assert.Throws<MapFormatException>(() => MapParser.Parse(new[] { "" }));
        }

        [Test]
        public void Parse_NullRowAfterTheFirst_Throws()
        {
            Assert.Throws<MapFormatException>(() => MapParser.Parse(new[] { "..", null }));
        }
    }
}
