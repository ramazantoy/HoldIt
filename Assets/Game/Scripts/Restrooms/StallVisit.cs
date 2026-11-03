using System;
using HoldIt.Paths;

namespace HoldIt.Restrooms
{
    /// <summary>A stall taken for a visitor, and the way in to it from the centre of its restroom's door.</summary>
    public sealed class StallVisit
    {
        public StallVisit(Gender gender, int stall, WalkLine line)
        {
            Gender = gender;
            Stall = stall;
            Line = line ?? throw new ArgumentNullException(nameof(line));
        }

        public Gender Gender { get; }
        public int Stall { get; }
        public WalkLine Line { get; }
    }
}
