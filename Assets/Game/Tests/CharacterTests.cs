using System;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Paths;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class CharacterTests
    {
        private static readonly Cell _start = new Cell(0, 10);

        /// <summary>Two tiles a second, so a quarter second is half a step.</summary>
        private Character _character;

        [SetUp]
        public void CreateCharacter() =>
            _character = new Character(new CharacterId(1), Gender.Female, BodySize.Normal, _start, 2f);

        [Test]
        public void New_IsIdleOnItsCellFacingTheCamera()
        {
            Assert.AreEqual(CharacterState.Idle, _character.State);
            Assert.AreEqual(_start, _character.Cell);
            Assert.AreEqual(_start, _character.Next);
            Assert.AreEqual(0f, _character.StepProgress);
            Assert.AreEqual(Direction.South, _character.Facing);
        }

        [Test]
        public void New_WithoutSpeed_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Character(new CharacterId(1), Gender.Male, BodySize.Fat, _start, 0f));
        }

        [Test]
        public void TryWalk_APathFromItsCell_StartsWalking()
        {
            Assert.IsTrue(_character.TryWalk(East(3)));
            Assert.AreEqual(CharacterState.Walking, _character.State);
        }

        [Test]
        public void TryWalk_APathFromAnotherCell_IsRefused()
        {
            Assert.IsFalse(_character.TryWalk(new CellPath(new[] { new Cell(1, 10), new Cell(2, 10) })));
            Assert.AreEqual(CharacterState.Idle, _character.State);
        }

        [Test]
        public void TryWalk_WhileWalking_IsRefused()
        {
            _character.TryWalk(East(3));

            Assert.IsFalse(_character.TryWalk(East(2)));
        }

        [Test]
        public void TryWalk_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _character.TryWalk(null));
        }

        [Test]
        public void Advance_MovesSpeedTimesTime()
        {
            _character.TryWalk(East(3));

            _character.Advance(0.25f);

            Assert.AreEqual(_start, _character.Cell);
            Assert.AreEqual(new Cell(1, 10), _character.Next);
            Assert.AreEqual(0.5f, _character.StepProgress, 1e-5f);
            Assert.AreEqual(Direction.East, _character.Facing);
        }

        [Test]
        public void Advance_CrossesCellsAndCarriesTheRest()
        {
            _character.TryWalk(East(4));

            _character.Advance(0.75f);

            Assert.AreEqual(new Cell(1, 10), _character.Cell);
            Assert.AreEqual(new Cell(2, 10), _character.Next);
            Assert.AreEqual(0.5f, _character.StepProgress, 1e-5f);
        }

        [Test]
        public void Advance_SmallTicksAddUpLikeOneLargeOne()
        {
            _character.TryWalk(East(4));

            for (var i = 0; i < 15; i++)
                _character.Advance(0.05f);

            Assert.AreEqual(new Cell(1, 10), _character.Cell);
            Assert.AreEqual(0.5f, _character.StepProgress, 1e-4f);
        }

        [Test]
        public void Advance_FacesEachStep()
        {
            _character.TryWalk(new CellPath(new[] { _start, new Cell(1, 10), new Cell(1, 9) }));

            _character.Advance(0.25f);
            Assert.AreEqual(Direction.East, _character.Facing);

            _character.Advance(0.5f);
            Assert.AreEqual(Direction.North, _character.Facing);
        }

        [Test]
        public void Advance_ToTheEnd_IsIdleOnTheLastCell()
        {
            _character.TryWalk(East(3));

            _character.Advance(1f);

            Assert.AreEqual(CharacterState.Idle, _character.State);
            Assert.AreEqual(new Cell(2, 10), _character.Cell);
            Assert.AreEqual(new Cell(2, 10), _character.Next);
            Assert.AreEqual(0f, _character.StepProgress);
        }

        [Test]
        public void Advance_PastTheEnd_StopsOnTheLastCell()
        {
            _character.TryWalk(East(3));

            _character.Advance(10f);

            Assert.AreEqual(CharacterState.Idle, _character.State);
            Assert.AreEqual(new Cell(2, 10), _character.Cell);
        }

        [Test]
        public void Advance_ByNothing_DoesNotMove()
        {
            _character.TryWalk(East(3));

            _character.Advance(0f);

            Assert.AreEqual(CharacterState.Walking, _character.State);
            Assert.AreEqual(_start, _character.Next);
            Assert.AreEqual(0f, _character.StepProgress);
        }

        [Test]
        public void Advance_WhileIdle_DoesNotMove()
        {
            _character.Advance(1f);

            Assert.AreEqual(_start, _character.Cell);
            Assert.AreEqual(_start, _character.Next);
        }

        [Test]
        public void Advance_BackInTime_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _character.Advance(-0.1f));
        }

        [Test]
        public void TryWalk_AfterArriving_StartsFromTheNewCell()
        {
            _character.TryWalk(East(2));
            _character.Advance(1f);

            Assert.IsTrue(_character.TryWalk(new CellPath(new[] { new Cell(1, 10), new Cell(1, 9) })));
        }

        /// <summary>A path east along row 10 from the start, <paramref name="count"/> cells long.</summary>
        private static CellPath East(int count)
        {
            var cells = new Cell[count];
            for (var i = 0; i < count; i++)
                cells[i] = new Cell(_start.Col + i, _start.Row);
            return new CellPath(cells);
        }
    }
}
