using HoldIt.Levels;

namespace HoldIt.Editor.Simulation
{
    /// <summary>How one simulated play of a level went.</summary>
    public readonly struct SimResult
    {
        /// <summary>Won or Lost; Playing when the time limit came first (the level got stuck).</summary>
        public readonly LevelOutcome Outcome;

        public readonly int Stars;
        public readonly int Served;
        public readonly int Accidents;

        /// <summary>Game seconds until the level ended, or the time limit.</summary>
        public readonly float Seconds;

        /// <summary>How full the patience of those served was as they went in, on average (0–1): how close the calls were.</summary>
        public readonly float MeanPatienceServed;

        /// <summary>The most characters on the board at once.</summary>
        public readonly int PeakBoard;

        /// <summary>Steps refused for the wrong door or a narrow passage; a good player makes none.</summary>
        public readonly int RuleRefusals;

        /// <summary>Steps given up because someone stood in the way too long.</summary>
        public readonly int Blocked;

        /// <summary>
        /// How much later than its schedule the last arrival came: time the spawner waited for a free entrance, lobby cell
        /// or room on the board.
        /// </summary>
        public readonly float ArrivalDelay;

        public SimResult(LevelOutcome outcome, int stars, int served, int accidents, float seconds,
            float meanPatienceServed, int peakBoard, int ruleRefusals, int blocked, float arrivalDelay)
        {
            Outcome = outcome;
            Stars = stars;
            Served = served;
            Accidents = accidents;
            Seconds = seconds;
            MeanPatienceServed = meanPatienceServed;
            PeakBoard = peakBoard;
            RuleRefusals = ruleRefusals;
            Blocked = blocked;
            ArrivalDelay = arrivalDelay;
        }

        public bool IsWon => Outcome == LevelOutcome.Won;
    }
}
