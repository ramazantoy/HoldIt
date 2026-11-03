using HoldIt.Board;
using HoldIt.Paths;

namespace HoldIt.Characters
{
    /// <summary>One character as the rest of the game sees it. Only <see cref="ICharacters"/> changes it.</summary>
    public interface ICharacter
    {
        CharacterId Id { get; }
        Gender Gender { get; }
        BodySize Size { get; }

        /// <summary>Tiles per second.</summary>
        float Speed { get; }

        CharacterState State { get; }

        /// <summary>The cell the character stands on, or is stepping out of.</summary>
        Cell Cell { get; }

        /// <summary>The cell the character is stepping into; the same as <see cref="Cell"/> between steps.</summary>
        Cell Next { get; }

        /// <summary>How far the current step from <see cref="Cell"/> to <see cref="Next"/> is, from 0 to 1.</summary>
        float StepProgress { get; }

        /// <summary>
        /// The side of the last cell it stepped into or asked to; South (toward the camera) before the first one.
        /// </summary>
        Direction Facing { get; }

        /// <summary>
        /// The cells being walked: the path while Walking, the way back while Returning; null while Idle. The character is
        /// on <c>Route[RouteIndex]</c>.
        /// </summary>
        CellPath Route { get; }

        /// <summary>Where on <see cref="Route"/> the character's <see cref="Cell"/> is.</summary>
        int RouteIndex { get; }
    }
}
