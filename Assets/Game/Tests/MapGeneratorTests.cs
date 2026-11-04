using System;
using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Editor.Maps;
using HoldIt.Restrooms;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace HoldIt.Tests
{
    public class MapGeneratorTests
    {
        private const int Seeds = 6;
        private const int Candidates = 3;
        private const int Attempts = 4000;

        private static readonly StallGeometry _geometry = StallGeometry.Default;
        private static readonly MapLimits _limits = MapLimits.Default;

        private static IEnumerable<TestCaseData> Bands()
        {
            foreach (var band in MapBand.Defaults())
                yield return new TestCaseData(band).SetName($"{band.Name} ({band.FirstLevel}-{band.LastLevel})");
        }

        [TestCaseSource(nameof(Bands))]
        public void EveryBand_MakesPlayableMapsThatFollowItsRecipe(MapBand band)
        {
            var recipe = band.Recipe;
            for (var seed = 1; seed <= Seeds; seed++)
            {
                var map = MapGenerator.Generate(recipe, _geometry, _limits, seed, Candidates, Attempts);
                var grid = MapParser.Parse(map.Rows);
                var where = $"seed {seed}:\n{string.Join("\n", map.Rows)}";

                CollectionAssert.IsEmpty(MapCheck.Problems(grid), where);
                foreach (Gender gender in Enum.GetValues(typeof(Gender)))
                    CollectionAssert.IsEmpty(RestroomLayout.Problems(grid, gender, map.Stalls, _geometry), where);
                AssertWithin(grid.Cols, recipe.Cols, "columns", where);
                AssertWithin(grid.Rows, recipe.Rows, "rows", where);
                AssertWithin(map.Stalls, recipe.Stalls, "stalls", where);
                Assert.LessOrEqual(grid.Cols, _limits.MaxCols, where);
                Assert.LessOrEqual(grid.Rows, _limits.MaxRows, where);

                var metrics = MapMetrics.Of(grid);
                AssertWithin(metrics.Entrances, recipe.Entrances, "entrances", where);
                AssertWithin(metrics.LobbyCells, recipe.Lobby, "lobby tiles", where);
                Assert.GreaterOrEqual(metrics.FatDetour + 1e-4f, recipe.FatDetour.x, where);
                Assert.LessOrEqual(metrics.FatDetour - 1e-4f, recipe.FatDetour.y, where);
                Assert.GreaterOrEqual(metrics.MeanPath + 1e-4f, recipe.MeanPath.x, where);
                Assert.LessOrEqual(metrics.MeanPath - 1e-4f, recipe.MeanPath.y, where);
            }
        }

        [Test]
        public void SameSeed_MakesTheSameMap()
        {
            var recipe = MapBand.Defaults()[3].Recipe;

            var first = MapGenerator.Generate(recipe, _geometry, _limits, 42, Candidates, Attempts);
            var second = MapGenerator.Generate(recipe, _geometry, _limits, 42, Candidates, Attempts);

            CollectionAssert.AreEqual(first.Rows, second.Rows);
            Assert.AreEqual(first.Stalls, second.Stalls);
            Assert.AreEqual(first.Attempt, second.Attempt);
        }

        [Test]
        public void OtherSeeds_MakeOtherMaps()
        {
            var recipe = MapBand.Defaults()[3].Recipe;
            var maps = new HashSet<string>();

            for (var seed = 1; seed <= 10; seed++)
                maps.Add(string.Join("|", MapGenerator.Generate(recipe, _geometry, _limits, seed, Candidates, Attempts).Rows));

            Assert.GreaterOrEqual(maps.Count, 9);
        }

        [Test]
        public void DoorsAtACorner_OpenOntoOneTileOnly()
        {
            // One stall: two tiles across, so the door is always at a corner of its restroom.
            var recipe = MapBand.Defaults()[5].Recipe;
            var random = new Random(7);
            var cornerDoors = 0;

            for (var i = 0; i < 200; i++)
            {
                var sketch = MapGenerator.Sketch(recipe, _geometry, random);
                if (sketch == null)
                    continue;
                var grid = MapParser.Parse(sketch.Rows);
                foreach (var problem in MapCheck.Problems(grid))
                    StringAssert.DoesNotContain("opens onto 2", problem, string.Join("\n", sketch.Rows));
                if (grid.BuildingOf(Gender.Male).width == 2 || grid.BuildingOf(Gender.Male).height == 2)
                    cornerDoors++;
            }

            Assert.Greater(cornerDoors, 20, "The recipe should make two-tile restrooms often.");
        }

        [Test]
        public void Symbols_AreTheParsersOwn()
        {
            var grid = MapParser.Parse(new[] { new string(MapSymbols.All) });

            Assert.AreEqual(new Tile(TileType.Floor), grid.At(new Cell(0, 0)));
            Assert.AreEqual(new Tile(TileType.Lobby), grid.At(new Cell(1, 0)));
            Assert.AreEqual(new Tile(TileType.Entrance), grid.At(new Cell(2, 0)));
            Assert.AreEqual(new Tile(TileType.Wall), grid.At(new Cell(3, 0)));
            Assert.AreEqual(new Tile(TileType.Narrow), grid.At(new Cell(4, 0)));
            Assert.AreEqual(new Tile(TileType.Building, Gender.Male), grid.At(new Cell(5, 0)));
            Assert.AreEqual(new Tile(TileType.Door, Gender.Male), grid.At(new Cell(6, 0)));
            Assert.AreEqual(new Tile(TileType.Building, Gender.Female), grid.At(new Cell(7, 0)));
            Assert.AreEqual(new Tile(TileType.Door, Gender.Female), grid.At(new Cell(8, 0)));
        }

        private static void AssertWithin(int value, Vector2Int range, string what, string where)
        {
            Assert.GreaterOrEqual(value, Mathf.Min(range.x, range.y), $"{what}, {where}");
            Assert.LessOrEqual(value, Mathf.Max(range.x, range.y), $"{what}, {where}");
        }
    }
}
