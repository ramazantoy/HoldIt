using HoldIt.Board;
using HoldIt.Levels;
using UnityEngine;

namespace HoldIt.Editor.Simulation
{
    /// <summary>
    /// How hard a level is, as one number: the slowest pace (seconds between paths) at which <see cref="SimPlayer"/> still
    /// wins the share of plays the config asks. A higher pace means an easier level. Paces are tried from the slowest down,
    /// and the first that wins is refined against the next slower one. A quicker player can always play slower, so a level
    /// counts as won at every pace quicker than that, even where the bot's own rush would lose it. Every pace is played with
    /// the same seeds, so the comparison is fair.
    /// </summary>
    public readonly struct Difficulty
    {
        /// <summary>The slowest winning pace found; 0 when even the fastest pace loses.</summary>
        public readonly float NeededPace;

        /// <summary>True when the slowest pace tried still wins: the level is at least that easy.</summary>
        public readonly bool EasierThanRange;

        /// <summary>The plays at the config's reference pace.</summary>
        public readonly SimBatch Reference;

        private Difficulty(float neededPace, bool easierThanRange, SimBatch reference)
        {
            NeededPace = neededPace;
            EasierThanRange = easierThanRange;
            Reference = reference;
        }

        public bool IsWinnable => NeededPace > 0f;

        public static Difficulty Measure(ITileGrid grid, int stallCount, LevelDefinition level, SimRules rules,
            LevelSimConfig config)
        {
            var reference = SimBatch.Play(grid, stallCount, level, rules, config.ReferencePace, config);
            bool Wins(float pace) => SimBatch.Play(grid, stallCount, level, rules, pace, config).WinShare >= config.WinShare;

            var slower = 0f;
            for (var step = 0; step < config.PaceSteps; step++)
            {
                var pace = Pace(step, config);
                if (!Wins(pace))
                {
                    slower = pace;
                    continue;
                }
                if (step == 0)
                    return new Difficulty(pace, true, reference);

                var winning = pace;
                for (var halving = 0; halving < config.RefineSteps; halving++)
                {
                    var middle = (winning + slower) * 0.5f;
                    if (Wins(middle))
                        winning = middle;
                    else
                        slower = middle;
                }
                return new Difficulty(winning, false, reference);
            }
            return new Difficulty(0f, false, reference);
        }

        // From the slowest (step 0) to the fastest, evenly on a log scale.
        private static float Pace(int step, LevelSimConfig config)
        {
            var t = step / (config.PaceSteps - 1f);
            return config.SlowestPace * Mathf.Pow(config.FastestPace / config.SlowestPace, t);
        }
    }
}
