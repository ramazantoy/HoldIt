using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Levels;

namespace HoldIt.Editor.Simulation
{
    /// <summary>A level played at one pace with seeds 1 to N, summed up.</summary>
    public readonly struct SimBatch
    {
        public readonly float Pace;
        public readonly int Plays;
        public readonly float WinShare;
        public readonly float MeanStars;
        public readonly float MeanServed;
        public readonly float MeanAccidents;
        public readonly float MeanPatienceServed;
        public readonly float MeanArrivalDelay;
        public readonly float MeanSeconds;
        public readonly int PeakBoard;
        public readonly int RuleRefusals;
        public readonly int Stuck;

        private SimBatch(float pace, IReadOnlyList<SimResult> results)
        {
            Pace = pace;
            Plays = results.Count;
            int wins = 0, stars = 0, served = 0, accidents = 0, peak = 0, refusals = 0, stuck = 0;
            float patience = 0f, delay = 0f, seconds = 0f;
            foreach (var result in results)
            {
                if (result.IsWon)
                    wins++;
                if (result.Outcome == LevelOutcome.Playing)
                    stuck++;
                stars += result.Stars;
                served += result.Served;
                accidents += result.Accidents;
                patience += result.MeanPatienceServed;
                delay += result.ArrivalDelay;
                seconds += result.Seconds;
                refusals += result.RuleRefusals;
                if (result.PeakBoard > peak)
                    peak = result.PeakBoard;
            }

            var count = results.Count > 0 ? results.Count : 1;
            WinShare = wins / (float)count;
            MeanStars = stars / (float)count;
            MeanServed = served / (float)count;
            MeanAccidents = accidents / (float)count;
            MeanPatienceServed = patience / count;
            MeanArrivalDelay = delay / count;
            MeanSeconds = seconds / count;
            PeakBoard = peak;
            RuleRefusals = refusals;
            Stuck = stuck;
        }

        public static SimBatch Play(ITileGrid grid, int stallCount, LevelDefinition level, SimRules rules, float pace,
            LevelSimConfig config)
        {
            var results = new List<SimResult>(config.Seeds);
            for (var seed = 1; seed <= config.Seeds; seed++)
                results.Add(LevelSim.Run(grid, stallCount, level, rules, pace, seed, config.TimeStep, config.TimeLimit));
            return new SimBatch(pace, results);
        }
    }
}
