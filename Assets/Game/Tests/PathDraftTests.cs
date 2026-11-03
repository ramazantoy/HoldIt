using System;
using HoldIt.Board;
using HoldIt.Paths;
using NUnit.Framework;

namespace HoldIt.Tests
{
    /// <summary>Drawing on Map 1 (<see cref="TestMaps.Lobby"/>).</summary>
    public class PathDraftTests
    {
        private PathDraft _draft;

        [SetUp]
        public void CreateDraft() => _draft = new PathDraft(MapParser.Parse(TestMaps.Lobby));

        [Test]
        public void Begin_HoldsOnlyTheStart()
        {
            _draft.Begin(new Cell(4, 10));

            CollectionAssert.AreEqual(new[] { new Cell(4, 10) }, _draft.Cells);
            Assert.AreEqual(new Cell(4, 10), _draft.Last);
            Assert.IsFalse(_draft.CanCommit);
        }

        [Test]
        public void Begin_OutsideTheMap_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _draft.Begin(new Cell(10, 10)));
        }

        [Test]
        public void ExtendToward_BeforeBegin_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _draft.ExtendToward(new Cell(4, 9)));
        }

        [Test]
        public void ExtendToward_ANeighbour_AddsIt()
        {
            _draft.Begin(new Cell(4, 10));

            Assert.IsTrue(_draft.ExtendToward(new Cell(4, 9)));
            Assert.That(_draft.Cells, Is.EqualTo(Cells((4, 10), (4, 9))));
            Assert.IsTrue(_draft.CanCommit);
        }

        [Test]
        public void ExtendToward_TheLastCell_ChangesNothing()
        {
            _draft.Begin(new Cell(4, 10));

            Assert.IsFalse(_draft.ExtendToward(new Cell(4, 10)));
            Assert.AreEqual(1, _draft.Cells.Count);
        }

        [Test]
        public void ExtendToward_AFarCell_FillsTheSteps()
        {
            _draft.Begin(new Cell(0, 10));

            _draft.ExtendToward(new Cell(0, 7));

            Assert.That(_draft.Cells, Is.EqualTo(Cells((0, 10), (0, 9), (0, 8), (0, 7))));
        }

        [Test]
        public void ExtendToward_ADiagonal_StepsTheLargerGapFirst()
        {
            _draft.Begin(new Cell(3, 10));
            _draft.ExtendToward(new Cell(5, 9));
            Assert.That(_draft.Cells, Is.EqualTo(Cells((3, 10), (4, 10), (5, 10), (5, 9))), "columns first");

            _draft.Begin(new Cell(3, 11));
            _draft.ExtendToward(new Cell(4, 9));
            Assert.That(_draft.Cells, Is.EqualTo(Cells((3, 11), (3, 10), (4, 10), (4, 9))), "rows first");
        }

        [Test]
        public void ExtendToward_ABlockedSide_TriesTheOtherOne()
        {
            // From (2, 9) the row step north is the wall at (2, 8), so it goes west; at (1, 9) only the wall is left.
            _draft.Begin(new Cell(3, 9));

            _draft.ExtendToward(new Cell(1, 7));

            Assert.That(_draft.Cells, Is.EqualTo(Cells((3, 9), (2, 9), (1, 9))));
        }

        [Test]
        public void ExtendToward_AWall_Stops()
        {
            _draft.Begin(new Cell(3, 9));

            Assert.IsFalse(_draft.ExtendToward(new Cell(3, 7)));
            Assert.AreEqual(1, _draft.Cells.Count);
        }

        [Test]
        public void ExtendToward_ABuilding_Stops()
        {
            _draft.Begin(new Cell(0, 4));

            Assert.IsFalse(_draft.ExtendToward(new Cell(0, 2)));
        }

        [Test]
        public void ExtendToward_AnEntrance_Stops()
        {
            _draft.Begin(new Cell(4, 12));

            Assert.IsFalse(_draft.ExtendToward(new Cell(4, 13)));
        }

        [Test]
        public void ExtendToward_ANarrowPassage_IsDrawn()
        {
            _draft.Begin(new Cell(4, 9));

            _draft.ExtendToward(new Cell(4, 7));

            Assert.That(_draft.Cells, Is.EqualTo(Cells((4, 9), (4, 8), (4, 7))));
        }

        [Test]
        public void ExtendToward_ADoor_EndsThePathThere()
        {
            _draft.Begin(new Cell(2, 4));

            _draft.ExtendToward(new Cell(2, 0));
            _draft.ExtendToward(new Cell(3, 3));

            Assert.That(_draft.Cells, Is.EqualTo(Cells((2, 4), (2, 3))));
        }

        [Test]
        public void ExtendToward_PastADoor_StopsAtTheDoor()
        {
            // A corner door with floor on two sides: the path could go on through it, but the door is its last tile.
            var draft = new PathDraft(MapParser.Parse(new[]
            {
                "MMM..",
                "MMm..",
                ".....",
            }));
            draft.Begin(new Cell(2, 2));

            draft.ExtendToward(new Cell(3, 0));

            Assert.That(draft.Cells, Is.EqualTo(Cells((2, 2), (2, 1))));
        }

        [Test]
        public void ExtendToward_EitherDoor_IsDrawn()
        {
            _draft.Begin(new Cell(7, 4));

            _draft.ExtendToward(new Cell(7, 3));

            Assert.AreEqual(new Cell(7, 3), _draft.Last);
        }

        [Test]
        public void ExtendToward_FromADoorBackToThePath_CutsTheDoor()
        {
            _draft.Begin(new Cell(2, 4));
            _draft.ExtendToward(new Cell(2, 3));

            Assert.IsTrue(_draft.ExtendToward(new Cell(2, 4)));
            Assert.That(_draft.Cells, Is.EqualTo(Cells((2, 4))));
        }

        [Test]
        public void ExtendToward_ThePreviousCell_RemovesTheLast()
        {
            _draft.Begin(new Cell(4, 10));
            _draft.ExtendToward(new Cell(4, 8));

            Assert.IsTrue(_draft.ExtendToward(new Cell(4, 9)));
            Assert.That(_draft.Cells, Is.EqualTo(Cells((4, 10), (4, 9))));
        }

        [Test]
        public void ExtendToward_ACellOnThePath_CutsBackToIt()
        {
            DrawLoop();

            _draft.ExtendToward(new Cell(3, 10));

            Assert.That(_draft.Cells, Is.EqualTo(Cells((3, 10))));
        }

        [Test]
        public void ExtendToward_AcrossItsOwnPath_CutsThenGoesOn()
        {
            DrawLoop();

            _draft.ExtendToward(new Cell(2, 10));

            Assert.That(_draft.Cells, Is.EqualTo(Cells((3, 10), (2, 10))));
        }

        [Test]
        public void ExtendToward_OutsideTheMap_StopsAtItsEdge()
        {
            _draft.Begin(new Cell(0, 10));

            Assert.IsFalse(_draft.ExtendToward(new Cell(-5, 10)), "left of column 0 is column 0");
            _draft.ExtendToward(new Cell(0, 99));

            Assert.That(_draft.Cells, Is.EqualTo(Cells((0, 10), (0, 11))), "the wall at (0, 12) stops it");
        }

        [Test]
        public void ToPath_BeforeTheFirstStep_Throws()
        {
            _draft.Begin(new Cell(4, 10));

            Assert.Throws<InvalidOperationException>(() => _draft.ToPath());
        }

        [Test]
        public void ToPath_HoldsTheDrawnCells()
        {
            _draft.Begin(new Cell(4, 10));
            _draft.ExtendToward(new Cell(5, 9));

            var path = _draft.ToPath();

            Assert.AreEqual(_draft.Cells.Count, path.Count);
            for (var i = 0; i < path.Count; i++)
                Assert.AreEqual(_draft.Cells[i], path[i]);
        }

        [Test]
        public void TrimEnd_DropsTheCellsItCannotEndOn()
        {
            DrawLoop();

            Assert.IsTrue(_draft.TrimEnd(cell => cell.Row != 10 || cell == new Cell(3, 10)));

            Assert.That(_draft.Cells, Is.EqualTo(Cells((3, 10), (3, 9), (4, 9))));
        }

        [Test]
        public void TrimEnd_StopsAtTheFirstCellItCanEndOn()
        {
            DrawLoop();

            Assert.IsTrue(_draft.TrimEnd(cell => cell == new Cell(3, 9)));

            Assert.That(_draft.Cells, Is.EqualTo(Cells((3, 10), (3, 9))));
        }

        [Test]
        public void TrimEnd_AnEndItCanEndOn_KeepsThePath()
        {
            DrawLoop();

            Assert.IsFalse(_draft.TrimEnd(cell => true));

            Assert.AreEqual(4, _draft.Cells.Count);
        }

        [Test]
        public void TrimEnd_NowhereToEnd_KeepsOnlyTheStart()
        {
            DrawLoop();

            _draft.TrimEnd(cell => false);

            Assert.That(_draft.Cells, Is.EqualTo(Cells((3, 10))));
            Assert.IsFalse(_draft.CanCommit);
        }

        [Test]
        public void TrimEnd_BeforeBeginOrWithoutARule_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _draft.TrimEnd(cell => true));
            _draft.Begin(new Cell(3, 10));
            Assert.Throws<ArgumentNullException>(() => _draft.TrimEnd(null));
        }

        /// <summary>(3, 10) north to (3, 9), east to (4, 9), south to (4, 10).</summary>
        private void DrawLoop()
        {
            _draft.Begin(new Cell(3, 10));
            _draft.ExtendToward(new Cell(3, 9));
            _draft.ExtendToward(new Cell(4, 9));
            _draft.ExtendToward(new Cell(4, 10));
            Assert.That(_draft.Cells, Is.EqualTo(Cells((3, 10), (3, 9), (4, 9), (4, 10))));
        }

        private static Cell[] Cells(params (int Col, int Row)[] cells) =>
            Array.ConvertAll(cells, cell => new Cell(cell.Col, cell.Row));
    }
}
