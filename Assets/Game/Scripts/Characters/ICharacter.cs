using HoldIt.Board;
using HoldIt.Paths;
using HoldIt.Restrooms;

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

        /// <summary>
        /// The cell the character stands on, or is stepping out of. InStall, the door it came in through: where it is then
        /// is on <see cref="Visit"/>'s line.
        /// </summary>
        Cell Cell { get; }

        /// <summary>The cell the character is stepping into; the same as <see cref="Cell"/> between steps.</summary>
        Cell Next { get; }

        /// <summary>How far the current step from <see cref="Cell"/> to <see cref="Next"/> is, from 0 to 1.</summary>
        float StepProgress { get; }

        /// <summary>
        /// The side of the last cell it stepped into or asked to; South (toward the camera) before the first one.
        /// </summary>
        Direction Facing { get; }

        /// <summary>The path being walked while Walking; null otherwise. The character is on <c>Route[RouteIndex]</c>.</summary>
        CellPath Route { get; }

        /// <summary>Where on <see cref="Route"/> the character's <see cref="Cell"/> is.</summary>
        int RouteIndex { get; }

        /// <summary>The stall it is walking into while InStall; null before.</summary>
        StallVisit Visit { get; }

        /// <summary>How far along <see cref="Visit"/>'s line it has walked, in world units.</summary>
        float VisitDistance { get; }

        /// <summary>
        /// How much of its patience is used up, from 0 when it came in to 1, an accident. It stops filling once the
        /// character is InStall.
        /// </summary>
        float Patience { get; }
    }
}
