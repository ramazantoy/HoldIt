using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AF.Core;
using HoldIt.Board;
using HoldIt.Editor.Maps;
using HoldIt.Levels;
using HoldIt.Restrooms;
using UnityEditor;

namespace HoldIt.Editor.Simulation
{
    /// <summary>
    /// "HoldIt/Levels/Simulate All": measures every level of the level list, its headroom against its target
    /// (<see cref="LevelTuner"/>) and the pace it needs (<see cref="Difficulty"/>), and writes the table to Logs/LevelSim.md
    /// next to each map's <see cref="MapMetrics"/>. "HoldIt/Levels/Fit to Targets" sets every level's gaps and most
    /// characters so that it has its target headroom.
    /// </summary>
    public static class LevelSimMenu
    {
        public const string REPORT_PATH = "Logs/LevelSim.md";

        private const string MENU = "HoldIt/Levels/Simulate All";
        private const string TITLE = "Simulate levels";
        private const string FIT_MENU = "HoldIt/Levels/Fit to Targets";
        private const string FIT_TITLE = "Fit levels to targets";

        [MenuItem(MENU)]
        private static void SimulateFromMenu()
        {
            var summary = SimulateAll(showProgress: true);
            EditorUtility.DisplayDialog(TITLE, summary, "OK");
        }

        [MenuItem(FIT_MENU)]
        private static void FitFromMenu()
        {
            if (!EditorUtility.DisplayDialog(FIT_TITLE,
                    "Every listed level's arrival gaps and most characters are overwritten: the level curve's, under the " +
                    "pressure that gives the level its target headroom. This takes a few minutes.", "Fit", "Cancel"))
                return;
            EditorUtility.DisplayDialog(FIT_TITLE, FitAll(showProgress: true), "OK");
        }

        /// <summary>
        /// Fits the listed levels' gaps and most characters (all when <paramref name="only"/> is empty) to their target
        /// headroom (<see cref="LevelTuner"/>), from the level curve's numbers, saves them and returns a summary.
        /// </summary>
        public static string FitAll(bool showProgress = false, params int[] only)
        {
            var list = AssetDatabase.LoadAssetAtPath<LevelList>(LevelGenerator.LIST_PATH);
            if (list == null)
                return $"There is no level list at {LevelGenerator.LIST_PATH}.";
            var config = SimRules.Find<LevelSimConfig>();
            var curve = SimRules.Find<LevelCurve>();
            var rules = SimRules.FromProject();

            var lines = new StringBuilder();
            var fitted = 0;
            try
            {
                for (var i = 0; i < list.Levels.Count && i < curve.Count; i++)
                {
                    if (only.Length > 0 && Array.IndexOf(only, i + 1) < 0)
                        continue;
                    var level = list.Levels[i];
                    if (showProgress && EditorUtility.DisplayCancelableProgressBar(FIT_TITLE, $"Level {i + 1}",
                            i / (float)list.Levels.Count))
                        break;
                    if (level == null || level.Map == null)
                        continue;
                    var grid = MapParser.Parse(level.Map.Rows);
                    if (!IsPlayable(grid, level.Map.StallsPerRestroom, rules))
                        continue;

                    var curveNumbers = curve.Ramp.At(i, curve.Count);
                    var target = config.TargetHeadroom(i, list.Levels.Count);
                    var winnable = LevelTuner.TryFit(grid, level.Map.StallsPerRestroom, level, curveNumbers.SpawnStart,
                        curveNumbers.SpawnEnd, curveNumbers.MaxCharacters, target, rules, config, out var numbers);
                    numbers.WriteTo(level);
                    AssetDatabase.SaveAssetIfDirty(level);
                    fitted++;
                    var line = string.Format(CultureInfo.InvariantCulture, "Level {0} ({1}): headroom {2:0.00}, {3}{4}",
                        i + 1, level.Map.name, target, numbers, winnable ? "" : "; not won under the least pressure tried");
                    lines.AppendLine(line);
                    Log.Info(line);
                }
            }
            finally
            {
                if (showProgress)
                    EditorUtility.ClearProgressBar();
            }
            return $"Fitted {fitted} levels.\n{lines}";
        }

        /// <summary>Simulates the listed levels (all when <paramref name="only"/> is empty), writes the report and returns a summary.</summary>
        public static string SimulateAll(bool showProgress = false, params int[] only)
        {
            var list = AssetDatabase.LoadAssetAtPath<LevelList>(LevelGenerator.LIST_PATH);
            if (list == null)
                return $"There is no level list at {LevelGenerator.LIST_PATH}.";
            var config = SimRules.Find<LevelSimConfig>();
            var rules = SimRules.FromProject();

            var rows = new List<string>();
            var offTarget = 0;
            var started = DateTime.Now;
            try
            {
                for (var i = 0; i < list.Levels.Count; i++)
                {
                    if (only.Length > 0 && Array.IndexOf(only, i + 1) < 0)
                        continue;
                    var level = list.Levels[i];
                    if (showProgress && EditorUtility.DisplayCancelableProgressBar(TITLE, $"Level {i + 1}",
                            i / (float)list.Levels.Count))
                        break;
                    var target = config.TargetHeadroom(i, list.Levels.Count);
                    var row = Measure(i + 1, level, rules, config, target, out var onTarget);
                    rows.Add(row);
                    if (!onTarget)
                        offTarget++;
                }
            }
            finally
            {
                if (showProgress)
                    EditorUtility.ClearProgressBar();
            }

            var report = Report(config, rows);
            Directory.CreateDirectory(Path.GetDirectoryName(REPORT_PATH) ?? ".");
            File.WriteAllText(REPORT_PATH, report);
            var summary = $"Simulated {rows.Count} levels in {(DateTime.Now - started).TotalSeconds:0} s; " +
                          $"{offTarget} off their target headroom. Table in {REPORT_PATH}.";
            Log.Info(summary);
            return summary;
        }

        private static string Measure(int number, LevelDefinition level, SimRules rules, LevelSimConfig config,
            float target, out bool onTarget)
        {
            onTarget = false;
            if (level == null || level.Map == null)
                return $"| {number} | — | no map |";

            ITileGrid grid;
            try
            {
                grid = MapParser.Parse(level.Map.Rows);
            }
            catch (MapFormatException exception)
            {
                return $"| {number} | {level.Map.name} | map does not parse: {exception.Message} |";
            }
            if (!IsPlayable(grid, level.Map.StallsPerRestroom, rules))
                return $"| {number} | {level.Map.name} | map is not playable (AF/Validate All) |";

            var metrics = MapMetrics.Of(grid);
            var headroom = LevelTuner.Measure(grid, level.Map.StallsPerRestroom, level, rules, config);
            var difficulty = Difficulty.Measure(grid, level.Map.StallsPerRestroom, level, rules, config);
            onTarget = headroom.IsWinnable && !headroom.AtLeast &&
                       Math.Abs(headroom.Value - target) <= target * config.TargetTolerance;
            var verdict = !headroom.IsWinnable ? "UNWINNABLE" :
                onTarget ? "ok" :
                headroom.Value > target ? "easy" : "hard";
            var reference = difficulty.Reference;
            var needed = !difficulty.IsWinnable ? "—" : string.Format(CultureInfo.InvariantCulture, "{0}{1:0.00}",
                difficulty.EasierThanRange ? "≥" : "", difficulty.NeededPace);

            return string.Format(CultureInfo.InvariantCulture,
                "| {0} | {1} | {2}x{3} | {4} | {5} | {6} | {7} | {8:0.0} | +{9:0.0} | {10} | {11} | {12:0.00}–{13:0.00} | " +
                "{14} | {15:0.00} | {16} | {17} | {18:0%} | {19:0.0} | {20:0.00} | {21:0.0} | {22} | {23} |",
                number, level.Map.name, metrics.Cols, metrics.Rows, level.Map.StallsPerRestroom, metrics.Entrances,
                metrics.LobbyCells, metrics.NarrowCells, metrics.MeanPath, metrics.FatDetour, level.Crowd,
                level.MaxCharacters, level.SpawnStart, level.SpawnEnd, headroom, target, verdict, needed,
                reference.WinShare, reference.MeanStars, reference.MeanPatienceServed, reference.MeanArrivalDelay,
                reference.PeakBoard, reference.Stuck);
        }

        private static bool IsPlayable(ITileGrid grid, int stallCount, SimRules rules)
        {
            if (MapCheck.Problems(grid).Count > 0)
                return false;
            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
            {
                if (RestroomLayout.Problems(grid, gender, stallCount, rules.Restrooms.Geometry).Count > 0)
                    return false;
            }
            return true;
        }

        private static string Report(LevelSimConfig config, IReadOnlyList<string> rows)
        {
            var text = new StringBuilder();
            text.AppendLine($"# Level simulation ({DateTime.Now:yyyy-MM-dd HH:mm})");
            text.AppendLine();
            text.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Headroom = how many times the tightest gaps a player drawing every {0:0.0} s still wins ({1:0%} of {2} " +
                "plays) the level's gaps are; target tolerance ±{3:0%}. Needed = the slowest pace that wins. @ref = played " +
                "at {0:0.0} s.", config.ReferencePace, config.WinShare, config.Seeds, config.TargetTolerance));
            text.AppendLine();
            text.AppendLine("| Lv | Map | Size | Stalls | Entr. | Lobby | Narrow | Path | Fat+ | Crowd | Max | Gaps | " +
                            "Headroom | Target | Verdict | Needed | Win@ref | Stars@ref | Patience@ref | Late s | Peak | Stuck |");
            text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var row in rows)
                text.AppendLine(row);
            return text.ToString();
        }
    }
}
