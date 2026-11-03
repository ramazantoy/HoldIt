using HoldIt.Board;
using HoldIt.Characters;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class CellClaimsTests
    {
        private static readonly Cell _cell = new Cell(2, 3);
        private static readonly CharacterId _first = new CharacterId(1);
        private static readonly CharacterId _second = new CharacterId(2);

        private CellClaims _claims;

        [SetUp]
        public void CreateClaims() => _claims = new CellClaims();

        [Test]
        public void New_EveryCellIsFree()
        {
            Assert.IsTrue(_claims.IsFree(_cell));
            Assert.IsTrue(_claims.IsFreeFor(_cell, _first));
            Assert.IsFalse(_claims.TryGetOccupant(_cell, out _));
            Assert.IsFalse(_claims.TryGetReserver(_cell, out _));
        }

        [Test]
        public void Occupy_HoldsTheCellForOneCharacterOnly()
        {
            Assert.IsTrue(_claims.Occupy(_cell, _first));
            Assert.IsTrue(_claims.Occupy(_cell, _first), "Holding it again is fine.");
            Assert.IsFalse(_claims.Occupy(_cell, _second));

            Assert.IsTrue(_claims.TryGetOccupant(_cell, out var occupant));
            Assert.AreEqual(_first, occupant);
            Assert.IsFalse(_claims.IsFree(_cell));
            Assert.IsTrue(_claims.IsFreeFor(_cell, _first));
            Assert.IsFalse(_claims.IsFreeFor(_cell, _second));
        }

        [Test]
        public void Vacate_ByTheOccupant_FreesTheCell()
        {
            _claims.Occupy(_cell, _first);

            _claims.Vacate(_cell, _first);

            Assert.IsTrue(_claims.IsFree(_cell));
            Assert.IsTrue(_claims.Occupy(_cell, _second));
        }

        [Test]
        public void Vacate_BySomeoneElse_KeepsTheOccupant()
        {
            _claims.Occupy(_cell, _first);

            _claims.Vacate(_cell, _second);

            Assert.IsTrue(_claims.TryGetOccupant(_cell, out var occupant));
            Assert.AreEqual(_first, occupant);
        }

        [Test]
        public void Reserve_KeepsTheCellForOneCharacterOnly()
        {
            Assert.IsTrue(_claims.Reserve(_cell, _first));
            Assert.IsFalse(_claims.Reserve(_cell, _second));

            Assert.IsTrue(_claims.TryGetReserver(_cell, out var reserver));
            Assert.AreEqual(_first, reserver);
            Assert.IsFalse(_claims.IsFree(_cell));
            Assert.IsTrue(_claims.IsFreeFor(_cell, _first));
            Assert.IsFalse(_claims.IsFreeFor(_cell, _second));
        }

        [Test]
        public void Reserve_DoesNotHoldTheCell()
        {
            _claims.Reserve(_cell, _first);

            Assert.IsTrue(_claims.Occupy(_cell, _second), "Someone may pass a reserved cell.");
        }

        [Test]
        public void UnreserveAll_DropsOnlyThatCharactersReservations()
        {
            var other = new Cell(5, 5);
            _claims.Reserve(_cell, _first);
            _claims.Reserve(new Cell(0, 0), _first);
            _claims.Reserve(other, _second);

            _claims.UnreserveAll(_first);

            Assert.IsTrue(_claims.IsFree(_cell));
            Assert.IsTrue(_claims.IsFree(new Cell(0, 0)));
            Assert.IsFalse(_claims.IsFree(other));
            Assert.IsTrue(_claims.Reserve(_cell, _second));
        }

        [Test]
        public void UnreserveAll_WithoutReservations_DoesNothing()
        {
            _claims.UnreserveAll(_first);

            Assert.IsTrue(_claims.IsFree(_cell));
        }
    }
}
