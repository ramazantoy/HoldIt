using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Characters;

namespace HoldIt.Drawing
{
    /// <summary>The path the player is drawing right now, if any. Read every frame by whatever shows it.</summary>
    public interface IPathDrawing
    {
        bool IsDrawing { get; }

        /// <summary>Whose path it is, while <see cref="IsDrawing"/>.</summary>
        CharacterId Drawer { get; }

        /// <summary>The cells so far, from the drawer's own; empty when not drawing.</summary>
        IReadOnlyList<Cell> Cells { get; }

        /// <summary>
        /// False while the path ends on a cell the drawer cannot end on (someone stands there or reserved it); letting go
        /// then cuts the path back to the last cell it can.
        /// </summary>
        bool EndIsFree { get; }
    }
}
