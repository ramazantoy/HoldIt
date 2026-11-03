using System;
using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Paths;
using HoldIt.Restrooms;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class CharacterTests
    {
        private const float BlockedWait = 0.6f;
        private const float StallSpeed = 2f;
        private static readonly Cell _start = new Cell(0, 10);

        /// <summary>Two tiles a second, so a quarter second is half a step.</summary>
        private Character _character;

        private ScriptedGate _gate;

        [SetUp]
        public void CreateCharacter()
        {
            _character = new Character(new CharacterId(1), Gender.Female, BodySize.Normal, _start, 2f, BlockedWait,
                StallSpeed);
            _gate = new ScriptedGate();
        }

        [Test]
        public void New_IsIdleOnItsCellFacingTheCamera()
        {
            Assert.AreEqual(CharacterState.Idle, _character.State);
            Assert.AreEqual(_start, _character.Cell);
            Assert.AreEqual(_start, _character.Next);
            Assert.AreEqual(0f, _character.StepProgress);
            Assert.AreEqual(Direction.South, _character.Facing);
            Assert.IsNull(_character.Route);
        }

        [Test]
        public void New_WithoutSpeedOrWithANegativeWait_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Character(new CharacterId(1), Gender.Male, BodySize.Fat, _start, 0f, BlockedWait, StallSpeed));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Character(new CharacterId(1), Gender.Male, BodySize.Fat, _start, 2f, -1f, StallSpeed));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Character(new CharacterId(1), Gender.Male, BodySize.Fat, _start, 2f, BlockedWait, 0f));
        }

        [Test]
        public void TryWalk_APathFromItsCell_StartsWalking()
        {
            var path = East(3);

            Assert.IsTrue(_character.TryWalk(path));
            Assert.AreEqual(CharacterState.Walking, _character.State);
            Assert.AreSame(path, _character.Route);
            Assert.AreEqual(0, _character.RouteIndex);
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

            Assert.AreEqual(StepRefusal.None, _character.Advance(0.25f, _gate));

            Assert.AreEqual(_start, _character.Cell);
            Assert.AreEqual(new Cell(1, 10), _character.Next);
            Assert.AreEqual(0.5f, _character.StepProgress, 1e-5f);
            Assert.AreEqual(Direction.East, _character.Facing);
        }

        [Test]
        public void Advance_CrossesCellsAndCarriesTheRest()
        {
            _character.TryWalk(East(4));

            _character.Advance(0.75f, _gate);

            Assert.AreEqual(new Cell(1, 10), _character.Cell);
            Assert.AreEqual(new Cell(2, 10), _character.Next);
            Assert.AreEqual(0.5f, _character.StepProgress, 1e-5f);
            Assert.AreEqual(1, _character.RouteIndex);
        }

        [Test]
        public void Advance_SmallTicksAddUpLikeOneLargeOne()
        {
            _character.TryWalk(East(4));

            for (var i = 0; i < 15; i++)
                _character.Advance(0.05f, _gate);

            Assert.AreEqual(new Cell(1, 10), _character.Cell);
            Assert.AreEqual(0.5f, _character.StepProgress, 1e-4f);
        }

        [Test]
        public void Advance_AsksTheGateBeforeEveryStepAndLeavesTheCellsBehind()
        {
            _character.TryWalk(East(3));

            _character.Advance(0.75f, _gate);

            CollectionAssert.AreEqual(new[] { new Cell(1, 10), new Cell(2, 10) }, _gate.Asked);
            CollectionAssert.AreEqual(new[] { _start }, _gate.Left);
        }

        [Test]
        public void Advance_FacesEachStep()
        {
            _character.TryWalk(new CellPath(new[] { _start, new Cell(1, 10), new Cell(1, 9) }));

            _character.Advance(0.25f, _gate);
            Assert.AreEqual(Direction.East, _character.Facing);

            _character.Advance(0.5f, _gate);
            Assert.AreEqual(Direction.North, _character.Facing);
        }

        [Test]
        public void Advance_ToTheEnd_IsIdleOnTheLastCellStillFacingThatWay()
        {
            _character.TryWalk(East(3));

            _character.Advance(1f, _gate);

            Assert.AreEqual(CharacterState.Idle, _character.State);
            Assert.AreEqual(new Cell(2, 10), _character.Cell);
            Assert.AreEqual(new Cell(2, 10), _character.Next);
            Assert.AreEqual(0f, _character.StepProgress);
            Assert.AreEqual(Direction.East, _character.Facing);
            Assert.IsNull(_character.Route);
        }

        [Test]
        public void Advance_PastTheEnd_StopsOnTheLastCell()
        {
            _character.TryWalk(East(3));

            _character.Advance(10f, _gate);

            Assert.AreEqual(CharacterState.Idle, _character.State);
            Assert.AreEqual(new Cell(2, 10), _character.Cell);
        }

        [Test]
        public void Advance_ByNothing_DoesNotMove()
        {
            _character.TryWalk(East(3));

            _character.Advance(0f, _gate);

            Assert.AreEqual(CharacterState.Walking, _character.State);
            Assert.AreEqual(_start, _character.Next);
            Assert.AreEqual(0f, _character.StepProgress);
        }

        [Test]
        public void Advance_WhileIdle_DoesNotMove()
        {
            _character.Advance(1f, _gate);

            Assert.AreEqual(_start, _character.Cell);
            Assert.AreEqual(_start, _character.Next);
            Assert.AreEqual(0, _gate.Asked.Count);
        }

        [Test]
        public void Advance_BackInTimeOrWithoutAGate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _character.Advance(-0.1f, _gate));
            Assert.Throws<ArgumentNullException>(() => _character.Advance(0.1f, null));
        }

        [Test]
        public void TryWalk_AfterArriving_StartsFromTheNewCell()
        {
            _character.TryWalk(East(2));
            _character.Advance(1f, _gate);

            Assert.IsTrue(_character.TryWalk(new CellPath(new[] { new Cell(1, 10), new Cell(1, 9) })));
        }

        [Test]
        public void Advance_BeforeAHeldCell_WaitsAtItsEdge()
        {
            _gate.Verdicts[new Cell(1, 10)] = StepVerdict.Wait;
            _character.TryWalk(East(3));

            Assert.AreEqual(StepRefusal.None, _character.Advance(0.3f, _gate));

            Assert.AreEqual(CharacterState.Walking, _character.State);
            Assert.AreEqual(_start, _character.Cell);
            Assert.AreEqual(_start, _character.Next);
            Assert.AreEqual(0f, _character.StepProgress);
            Assert.AreEqual(Direction.East, _character.Facing, "It faces the cell it waits for.");
        }

        [Test]
        public void Advance_WhenTheHeldCellFrees_GoesOn()
        {
            _gate.Verdicts[new Cell(1, 10)] = StepVerdict.Wait;
            _character.TryWalk(East(3));
            _character.Advance(0.3f, _gate);

            _gate.Verdicts.Remove(new Cell(1, 10));
            _character.Advance(0.25f, _gate);

            Assert.AreEqual(new Cell(1, 10), _character.Next);
            Assert.AreEqual(0.5f, _character.StepProgress, 1e-5f);
        }

        [Test]
        public void Advance_WaitingTooLong_StopsWhereItIsAsBlocked()
        {
            _gate.Verdicts[new Cell(1, 10)] = StepVerdict.Wait;
            _character.TryWalk(East(3));

            Assert.AreEqual(StepRefusal.None, _character.Advance(BlockedWait * 0.5f, _gate));
            Assert.AreEqual(StepRefusal.Blocked, _character.Advance(BlockedWait * 0.5f, _gate));

            Assert.AreEqual(CharacterState.Idle, _character.State);
            Assert.AreEqual(_start, _character.Cell);
            Assert.IsNull(_character.Route);
        }

        [Test]
        public void Advance_TimeLeftAfterAStep_GoesIntoTheWait()
        {
            _gate.Verdicts[new Cell(2, 10)] = StepVerdict.Wait;
            _character.TryWalk(East(3));

            Assert.AreEqual(StepRefusal.Blocked, _character.Advance(0.5f + BlockedWait + 0.01f, _gate));
            Assert.AreEqual(new Cell(1, 10), _character.Cell);
        }

        [Test]
        public void Advance_ARefusedStep_StopsWhereItIsAndCanBeSentAgain()
        {
            _gate.Verdicts[new Cell(3, 10)] = StepVerdict.WrongDoor;
            _character.TryWalk(East(4));

            Assert.AreEqual(StepRefusal.WrongDoor, _character.Advance(1.1f, _gate));

            Assert.AreEqual(CharacterState.Idle, _character.State);
            Assert.AreEqual(new Cell(2, 10), _character.Cell);
            Assert.AreEqual(new Cell(2, 10), _character.Next);
            Assert.AreEqual(0f, _character.StepProgress);
            Assert.AreEqual(Direction.East, _character.Facing, "It faces where it could not go.");
            Assert.IsNull(_character.Route);

            Assert.AreEqual(StepRefusal.None, _character.Advance(10f, _gate));
            Assert.AreEqual(new Cell(2, 10), _character.Cell);
            Assert.IsTrue(_character.TryWalk(new CellPath(new[] { new Cell(2, 10), new Cell(2, 9) })));
        }

        [Test]
        public void Advance_RefusedOnTheFirstStep_StaysIdleWhereItIs()
        {
            _gate.Verdicts[new Cell(1, 10)] = StepVerdict.TooWide;
            _character.TryWalk(East(3));

            Assert.AreEqual(StepRefusal.TooWide, _character.Advance(0.1f, _gate));

            Assert.AreEqual(CharacterState.Idle, _character.State);
            Assert.AreEqual(_start, _character.Cell);
            Assert.IsNull(_character.Route);
            Assert.AreEqual(Direction.East, _character.Facing, "It faces where it could not go.");
        }

        [Test]
        public void Queue_WhenIdle_WaitsWhereItIs()
        {
            _character.Queue();

            Assert.AreEqual(CharacterState.Queued, _character.State);
            Assert.AreEqual(StepRefusal.None, _character.Advance(5f, _gate));
            Assert.AreEqual(CharacterState.Queued, _character.State);
            Assert.AreEqual(_start, _character.Cell);
            Assert.AreEqual(0, _gate.Asked.Count);
        }

        [Test]
        public void Queue_UnlessIdle_Throws()
        {
            _character.TryWalk(East(3));

            Assert.Throws<InvalidOperationException>(() => _character.Queue());
            Assert.AreEqual(CharacterState.Walking, _character.State);
        }

        [Test]
        public void Queue_Twice_Throws()
        {
            _character.Queue();

            Assert.Throws<InvalidOperationException>(() => _character.Queue());
        }

        [Test]
        public void TryWalk_QueuedOrInAStall_IsRefused()
        {
            _character.Queue();
            Assert.IsFalse(_character.TryWalk(East(3)));
            Assert.AreEqual(CharacterState.Queued, _character.State);

            _character.EnterStall(Visit(3f));
            Assert.IsFalse(_character.TryWalk(East(3)));
            Assert.AreEqual(CharacterState.InStall, _character.State);
        }

        [Test]
        public void EnterStall_WithoutAVisit_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _character.EnterStall(null));
            Assert.AreEqual(CharacterState.Idle, _character.State);
        }

        [Test]
        public void EnterStall_FromIdleOrQueued_StartsAtTheLinesStart()
        {
            var visit = Visit(3f);
            _character.Queue();

            _character.EnterStall(visit);

            Assert.AreEqual(CharacterState.InStall, _character.State);
            Assert.AreSame(visit, _character.Visit);
            Assert.AreEqual(0f, _character.VisitDistance);
            Assert.IsNull(_character.Route);
            Assert.AreEqual(_start, _character.Cell);
            Assert.IsFalse(_character.IsInside);
        }

        [Test]
        public void EnterStall_WhileWalking_Throws()
        {
            _character.TryWalk(East(3));

            Assert.Throws<InvalidOperationException>(() => _character.EnterStall(Visit(3f)));
            Assert.IsNull(_character.Visit);
        }

        [Test]
        public void Advance_InStall_WalksTheLineAtTheStallSpeedWithoutAskingTheGate()
        {
            _character.EnterStall(Visit(3f));

            _character.Advance(0.5f, _gate);

            Assert.AreEqual(0.5f * StallSpeed, _character.VisitDistance, 1e-5f);
            Assert.IsFalse(_character.IsInside);
            Assert.AreEqual(0, _gate.Asked.Count);
            Assert.AreEqual(0, _gate.Left.Count);
        }

        [Test]
        public void Advance_InStall_StopsAtTheLinesEndAndIsInside()
        {
            _character.EnterStall(Visit(3f));

            _character.Advance(10f, _gate);

            Assert.AreEqual(3f, _character.VisitDistance, 1e-5f);
            Assert.IsTrue(_character.IsInside);
            Assert.AreEqual(CharacterState.InStall, _character.State);
        }

        /// <summary>A visit to stall 0 whose way in is a straight line <paramref name="length"/> long.</summary>
        private static StallVisit Visit(float length) =>
            new StallVisit(Gender.Female, 0, new WalkLine(new[] { Vector3.zero, Vector3.forward * length }));

        /// <summary>A path east along row 10 from the start, <paramref name="count"/> cells long.</summary>
        private static CellPath East(int count)
        {
            var cells = new Cell[count];
            for (var i = 0; i < count; i++)
                cells[i] = new Cell(_start.Col + i, _start.Row);
            return new CellPath(cells);
        }

        /// <summary>Lets the character into every cell except those given a verdict; records what it was asked.</summary>
        private sealed class ScriptedGate : IStepGate
        {
            public readonly Dictionary<Cell, StepVerdict> Verdicts = new Dictionary<Cell, StepVerdict>();
            public readonly List<Cell> Asked = new List<Cell>();
            public readonly List<Cell> Left = new List<Cell>();

            public StepVerdict TryEnter(ICharacter character, Cell next)
            {
                Asked.Add(next);
                return Verdicts.TryGetValue(next, out var verdict) ? verdict : StepVerdict.Enter;
            }

            public void Leave(ICharacter character, Cell cell) => Left.Add(cell);
        }
    }
}
