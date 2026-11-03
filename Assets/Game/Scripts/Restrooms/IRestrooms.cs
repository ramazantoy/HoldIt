using HoldIt.Paths;

namespace HoldIt.Restrooms
{
    /// <summary>
    /// The restrooms' stalls, one restroom a gender, each with <see cref="StallCount"/> stalls. A stall is taken for a
    /// visitor at the door, shut once the visitor is in, and freed by time. Every change is announced after it is made
    /// (<see cref="StallStateChanged"/>).
    /// </summary>
    public interface IRestrooms
    {
        /// <summary>How many stalls each restroom has.</summary>
        int StallCount { get; }

        StallState StateOf(Gender gender, int stall);

        /// <summary>Where the restroom's stalls and their parts stand.</summary>
        RestroomLayout LayoutOf(Gender gender);

        /// <summary>
        /// Takes the restroom's first free stall for a visitor at its door and gives the way in (a
        /// <see cref="WalkLine"/> from the door's centre). False, and nothing changes, when every stall is busy.
        /// </summary>
        bool TryTake(Gender gender, out StallVisit visit);

        /// <summary>The visitor of a taken stall is in: its door closes. Throws for a stall that is not taken.</summary>
        void Shut(Gender gender, int stall);
    }
}
