using System.Collections.Generic;
using HoldIt.Board;

namespace HoldIt.Characters
{
    /// <summary>
    /// Who is on which cell and who will be. A cell holds at most one character (occupancy): the one standing on it, or
    /// stepping into it. A cell can also be reserved by one character for later (reservation): where its path will end.
    /// Reservations keep two characters from being sent to the same place.
    /// </summary>
    internal sealed class CellClaims
    {
        private readonly Dictionary<Cell, CharacterId> _occupants = new Dictionary<Cell, CharacterId>();
        private readonly Dictionary<Cell, CharacterId> _reservations = new Dictionary<Cell, CharacterId>();
        private readonly Dictionary<CharacterId, List<Cell>> _reservedBy = new Dictionary<CharacterId, List<Cell>>();

        public bool TryGetOccupant(Cell cell, out CharacterId id) => _occupants.TryGetValue(cell, out id);

        public bool TryGetReserver(Cell cell, out CharacterId id) => _reservations.TryGetValue(cell, out id);

        /// <summary>Holds the cell for the character. False, and nothing changes, when someone else holds it.</summary>
        public bool Occupy(Cell cell, CharacterId id)
        {
            if (_occupants.TryGetValue(cell, out var occupant))
                return occupant == id;
            _occupants.Add(cell, id);
            return true;
        }

        /// <summary>Lets go of the cell if this character holds it.</summary>
        public void Vacate(Cell cell, CharacterId id)
        {
            if (_occupants.TryGetValue(cell, out var occupant) && occupant == id)
                _occupants.Remove(cell);
        }

        /// <summary>Keeps the cell for the character. False, and nothing changes, when someone else reserved it.</summary>
        public bool Reserve(Cell cell, CharacterId id)
        {
            if (_reservations.TryGetValue(cell, out var reserver))
                return reserver == id;
            _reservations.Add(cell, id);
            if (!_reservedBy.TryGetValue(id, out var cells))
                _reservedBy.Add(id, cells = new List<Cell>());
            cells.Add(cell);
            return true;
        }

        /// <summary>Drops every reservation of the character, and the character with them until it reserves again.</summary>
        public void UnreserveAll(CharacterId id)
        {
            if (!_reservedBy.TryGetValue(id, out var cells))
                return;
            foreach (var cell in cells)
                _reservations.Remove(cell);
            _reservedBy.Remove(id);
        }

        /// <summary>True when nobody holds or reserved the cell.</summary>
        public bool IsFree(Cell cell) => !_occupants.ContainsKey(cell) && !_reservations.ContainsKey(cell);

        /// <summary>True when nobody else holds or reserved the cell.</summary>
        public bool IsFreeFor(Cell cell, CharacterId id) =>
            (!_occupants.TryGetValue(cell, out var occupant) || occupant == id) &&
            (!_reservations.TryGetValue(cell, out var reserver) || reserver == id);
    }
}
