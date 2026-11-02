using System;

namespace HoldIt.Board
{
    /// <summary>A map's text cannot be read as tiles: no rows, uneven rows, or an unknown character.</summary>
    public sealed class MapFormatException : Exception
    {
        public MapFormatException(string message) : base(message)
        {
        }
    }
}
