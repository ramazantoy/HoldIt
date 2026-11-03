using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Paths;

namespace HoldIt.Characters
{
    /// <summary>
    /// The characters on the board. Changes are announced after they are made: <see cref="CharacterSpawned"/>,
    /// <see cref="CharacterStateChanged"/>, <see cref="StepRefused"/> and <see cref="CharacterRemoved"/>. Positions change
    /// every frame and are read from <see cref="ICharacter"/>. No two characters ever share a cell, and no two are sent to
    /// end on the same one. A character that reaches its restroom's door goes into a free stall (InStall) or waits on the
    /// door (Queued); once in the stall it is removed.
    /// </summary>
    public interface ICharacters
    {
        /// <summary>In the order they came; a removed character is no longer here.</summary>
        IReadOnlyList<ICharacter> All { get; }

        bool TryGet(CharacterId id, out ICharacter character);

        /// <summary>
        /// Adds an Idle character on a walkable cell, walking at its size's speed. Throws for a cell that is not walkable,
        /// is a door, or is not <see cref="IsFree"/>.
        /// </summary>
        CharacterId Spawn(Gender gender, BodySize size, Cell cell);

        /// <summary>
        /// Starts the character on the path and reserves its end (unless a door) while it walks. False when there is no
        /// such character, it is not Idle, the path does not start on its cell, or the character cannot end on the path's
        /// last cell (<see cref="CanEndOn"/>).
        /// </summary>
        bool TryWalk(CharacterId id, CellPath path);

        /// <summary>
        /// True when a path of this character may end on the cell: a door (its queue is the restroom's business), or a cell
        /// nobody else stands on or reserved.
        /// </summary>
        bool CanEndOn(CharacterId id, Cell cell);

        /// <summary>True for a cell on the map that nobody stands on or reserved.</summary>
        bool IsFree(Cell cell);
    }
}
