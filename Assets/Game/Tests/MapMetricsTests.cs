using HoldIt.Board;
using HoldIt.Editor.Maps;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class MapMetricsTests
    {
        // Men's door (1,1), women's (4,1). The narrow cell (2,3) is the men's short way to the lobby; Fat ones go round by
        // (4,3) and (5,3).
        private static readonly string[] _shortcut =
        {
            "MMMWWW",
            "MmMWwW",
            "......",
            "##n#..",
            "LLLLLL",
            "##EE##",
        };

        [Test]
        public void SmallMap_IsMeasuredStepByStep()
        {
            var metrics = MapMetrics.Of(MapParser.Parse(TestMaps.Small));

            Assert.AreEqual(6, metrics.Cols);
            Assert.AreEqual(5, metrics.Rows);
            Assert.AreEqual(1, metrics.Entrances, "Side by side entrance tiles are one entrance.");
            Assert.AreEqual(6, metrics.LobbyCells);
            Assert.AreEqual(0, metrics.NarrowCells);
            // From lobby cell c: 2 + |c - door column|, for both doors: 23 steps over 6 cells each.
            Assert.AreEqual(23f / 6f, metrics.MeanPath, 1e-4f);
            Assert.AreEqual(metrics.MeanPath, metrics.MeanFatPath, 1e-4f);
            Assert.AreEqual(0f, metrics.FatDetour, 1e-4f);
            Assert.AreEqual(6, metrics.LongestPath);
        }

        [Test]
        public void NarrowShortcut_SendsFatCharactersRound()
        {
            var grid = MapParser.Parse(_shortcut);
            Assert.IsEmpty(MapCheck.Problems(grid));

            var metrics = MapMetrics.Of(grid);

            Assert.AreEqual(1, metrics.NarrowCells);
            // Men 33 + women 29 steps for anyone; men 47 + women 29 for Fat (the women's door does not need the shortcut).
            Assert.AreEqual(62f / 12f, metrics.MeanPath, 1e-4f);
            Assert.AreEqual(76f / 12f, metrics.MeanFatPath, 1e-4f);
            Assert.AreEqual(14f / 12f, metrics.FatDetour, 1e-4f);
            Assert.AreEqual(10, metrics.LongestPath, "A Fat man from the far lobby corner.");
        }

        [Test]
        public void EntrancesApart_AreCountedApart()
        {
            var grid = MapParser.Parse(new[]
            {
                "MMMWWW",
                "MmMWwW",
                "......",
                "LLLLLL",
                "E####E",
            });

            Assert.AreEqual(2, MapMetrics.Of(grid).Entrances);
        }
    }
}
