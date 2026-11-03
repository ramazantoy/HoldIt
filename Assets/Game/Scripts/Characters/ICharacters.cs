using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Paths;

namespace HoldIt.Characters
{
    /// <summary>
    /// The characters on the board. Changes are announced after they are made: <see cref="CharacterSpawned"/> and
    /// <see cref="CharacterStateChanged"/>. Positions change every frame and are read from <see cref="ICharacter"/>.
    /// </summary>
    public interface ICharacters
    {
        /// <summary>In the order they came.</summary>
        IReadOnlyList<ICharacter> All { get; }

        bool TryGet(CharacterId id, out ICharacter character);

        /// <summary>
        /// Adds an Idle character on a walkable cell, walking at its size's speed. Throws for a cell that is not walkable.
        /// </summary>
        CharacterId Spawn(Gender gender, BodySize size, Cell cell);

        /// <summary>
        /// Starts the character on the path. False when there is no such character, it is not Idle, or the path does not
        /// start on its cell.
        /// </summary>
        bool TryWalk(CharacterId id, CellPath path);
    }
}
