using System;
using System.Globalization;
using HoldIt.Board;
using HoldIt.Levels;
using UnityEngine;

namespace HoldIt.Editor.Simulation
{
    /// <summary>
    /// How much more pressure a level takes before a player drawing at the config's reference pace stops winning the
    /// config's share of plays. Pressure p divides both arrival gaps and multiplies the most characters on the board, so it
    /// bites whether the player's pace, the board's room or the stalls run out first: fewer stalls come out with wider
    /// gaps, a fast-flowing map with a fuller board. More pressure never makes a level easier, which the halving of the
    /// pressure range counts on. The most pressure a level takes, relative to its own numbers, is its headroom; a level is
    /// fitted to a target headroom by setting its pressure to the most it takes over the target.
    /// </summary>
    public static class LevelTuner
    {
        /// <summary>A level's headroom: 0 when the reference player cannot win it even under the least pressure tried.</summary>
        public readonly struct Headroom
        {
            public readonly float Value;

            /// <summary>True when even the most pressure tried still wins: the headroom is at least <see cref="Value"/>.</summary>
            public readonly bool AtLeast;

            public Headroom(float value, bool atLeast)
            {
                Value = value;
                AtLeast = atLeast;
            }

            public bool IsWinnable => Value > 0f;

            public override string ToString() => !IsWinnable ? "none" :
                string.Format(CultureInfo.InvariantCulture, "{0}{1:0.00}", AtLeast ? "≥" : "", Value);
        }

        /// <summary>A level's numbers under some pressure: gaps in hundredths of a second, the most characters rounded.</summary>
        public readonly struct Numbers
        {
            public readonly float SpawnStart;
            public readonly float SpawnEnd;
            public readonly int MaxCharacters;
            public readonly float Pressure;

            public Numbers(float spawnStart, float spawnEnd, int maxCharacters, float pressure)
            {
                SpawnStart = spawnStart;
                SpawnEnd = spawnEnd;
                MaxCharacters = maxCharacters;
                Pressure = pressure;
            }

            public static Numbers Under(float pressure, float baseStart, float baseEnd, int baseMax)
            {
                var start = Hundredths(baseStart / pressure);
                return new Numbers(start, Mathf.Min(start, Hundredths(baseEnd / pressure)),
                    Mathf.Max(1, Mathf.RoundToInt(baseMax * pressure)), pressure);
            }

            public void WriteTo(LevelDefinition level) =>
                LevelGenerator.WriteNumbers(level, SpawnStart, SpawnEnd, MaxCharacters);

            public override string ToString() => string.Format(CultureInfo.InvariantCulture,
                "gaps {0:0.00}–{1:0.00} s, at most {2} (pressure ×{3:0.00})", SpawnStart, SpawnEnd, MaxCharacters, Pressure);
        }

        /// <summary>The level's headroom with its own numbers.</summary>
        public static Headroom Measure(ITileGrid grid, int stallCount, LevelDefinition level, SimRules rules,
            LevelSimConfig config)
        {
            var most = MostPressure(grid, stallCount, level, level.SpawnStart, level.SpawnEnd, level.MaxCharacters, rules,
                config, out var winnable, out var atMost);
            return winnable ? new Headroom(most, atMost) : new Headroom(0f, false);
        }

        /// <summary>
        /// The level's numbers with <paramref name="headroom"/>: the base numbers under the most pressure the reference
        /// player wins, divided by the headroom. A level won even under the most pressure tried is easy because of its
        /// small crowd, whatever its gaps: it keeps the base numbers. False when the level cannot be won under any pressure
        /// tried (it gets the least). The level itself is not changed.
        /// </summary>
        public static bool TryFit(ITileGrid grid, int stallCount, LevelDefinition level, float baseStart, float baseEnd,
            int baseMax, float headroom, SimRules rules, LevelSimConfig config, out Numbers numbers)
        {
            var most = MostPressure(grid, stallCount, level, baseStart, baseEnd, baseMax, rules, config, out var winnable,
                out var atMost);
            var pressure = !winnable ? config.LeastPressure : atMost ? 1f : most / headroom;
            numbers = Numbers.Under(pressure, baseStart, baseEnd, baseMax);
            return winnable;
        }

        private static float MostPressure(ITileGrid grid, int stallCount, LevelDefinition level, float baseStart,
            float baseEnd, int baseMax, SimRules rules, LevelSimConfig config, out bool winnable, out bool atMost)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));
            if (baseStart <= 0f || baseEnd <= 0f || baseMax < 1)
                throw new ArgumentOutOfRangeException(nameof(baseStart), "The gaps and the most characters must be positive.");

            var trial = UnityEngine.Object.Instantiate(level);
            try
            {
                bool Wins(float pressure)
                {
                    Numbers.Under(pressure, baseStart, baseEnd, baseMax).WriteTo(trial);
                    return SimBatch.Play(grid, stallCount, trial, rules, config.ReferencePace, config).WinShare >=
                           config.WinShare;
                }

                var easy = config.LeastPressure;
                var hard = config.MostPressure;
                atMost = Wins(hard);
                winnable = atMost || Wins(easy);
                if (atMost || !winnable)
                    return atMost ? hard : easy;

                // Halve on a log scale: the pressure that wins stays the easy end.
                for (var step = 0; step < config.PressureSteps; step++)
                {
                    var middle = Mathf.Sqrt(easy * hard);
                    if (Wins(middle))
                        easy = middle;
                    else
                        hard = middle;
                }
                return easy;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(trial);
            }
        }

        private static float Hundredths(float value) => Mathf.Round(value * 100f) / 100f;
    }
}
