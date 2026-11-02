using System.Collections.Generic;
using System.Linq;
using HoldIt.Board;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class BoardLayoutTests
    {
        [Test]
        public void EveryTileGetsItsPiece_AndEachRestroomNineWalls()
        {
            var pieces = Build(TestMaps.Small);

            Assert.AreEqual(6, Count(pieces, PieceKind.Floor));
            Assert.AreEqual(6, Count(pieces, PieceKind.LobbyFloor));
            Assert.AreEqual(2, Count(pieces, PieceKind.EntranceFloor));
            Assert.AreEqual(4, Count(pieces, PieceKind.WallBlock));
            Assert.AreEqual(0, Count(pieces, PieceKind.NarrowPosts));
            Assert.AreEqual(5, Count(pieces, PieceKind.BuildingFloor, Gender.Male));
            Assert.AreEqual(5, Count(pieces, PieceKind.BuildingFloor, Gender.Female));
            Assert.AreEqual(1, Count(pieces, PieceKind.Doorway, Gender.Male));
            Assert.AreEqual(1, Count(pieces, PieceKind.Doorway, Gender.Female));
            // A 3 x 2 rectangle has 10 outer edges; the door's opening has none.
            Assert.AreEqual(9, Count(pieces, PieceKind.BuildingWall, Gender.Male));
            Assert.AreEqual(9, Count(pieces, PieceKind.BuildingWall, Gender.Female));
        }

        [Test]
        public void Doorway_FacesTheTileItOpensTo()
        {
            var pieces = Build(TestMaps.Small);

            var doorway = pieces.Single(piece => piece.Kind == PieceKind.Doorway && piece.Gender == Gender.Male);
            Assert.AreEqual(new Cell(1, 1), doorway.Cell);
            Assert.AreEqual(Direction.South, doorway.Facing);
        }

        [Test]
        public void BuildingWalls_RunAroundTheRectangle_ExceptTheOpening()
        {
            var pieces = Build(TestMaps.Small);

            CollectionAssert.AreEquivalent(new[] { Direction.North, Direction.West }, WallsAt(pieces, new Cell(0, 0)));
            CollectionAssert.AreEquivalent(new[] { Direction.North }, WallsAt(pieces, new Cell(1, 0)));
            CollectionAssert.AreEquivalent(new[] { Direction.North, Direction.East }, WallsAt(pieces, new Cell(2, 0)));
            CollectionAssert.AreEquivalent(new[] { Direction.West, Direction.South }, WallsAt(pieces, new Cell(0, 1)));
            CollectionAssert.IsEmpty(WallsAt(pieces, new Cell(1, 1)));
            CollectionAssert.AreEquivalent(new[] { Direction.East, Direction.South }, WallsAt(pieces, new Cell(2, 1)));
        }

        [Test]
        public void NarrowPassage_StandsOnFloor_AcrossItsPassage()
        {
            // (2, 2) runs north-south (its south side is open); (2, 5) runs east-west (wall north and south).
            var pieces = Build("MMMWWW", "MmMWwW", "..n...", "LLLLLL", "LL#LLL", "LLnLLL", "LL#LLL", "##EE##");

            var posts = pieces.Where(piece => piece.Kind == PieceKind.NarrowPosts).ToList();
            Assert.AreEqual(2, posts.Count);
            Assert.AreEqual(Direction.North, posts.Single(piece => piece.Cell == new Cell(2, 2)).Facing);
            Assert.AreEqual(Direction.East, posts.Single(piece => piece.Cell == new Cell(2, 5)).Facing);
            Assert.IsTrue(pieces.Any(piece => piece.Kind == PieceKind.Floor && piece.Cell == new Cell(2, 5)));
        }

        private static List<PiecePlacement> Build(params string[] rows) => BoardLayout.Build(MapParser.Parse(rows));

        private static int Count(List<PiecePlacement> pieces, PieceKind kind, Gender? gender = null) =>
            pieces.Count(piece => piece.Kind == kind && piece.Gender == gender);

        private static List<Direction> WallsAt(List<PiecePlacement> pieces, Cell cell) =>
            pieces.Where(piece => piece.Kind == PieceKind.BuildingWall && piece.Cell == cell)
                .Select(piece => piece.Facing)
                .ToList();
    }
}
