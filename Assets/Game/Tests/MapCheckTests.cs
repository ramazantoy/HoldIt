using System.Collections.Generic;
using HoldIt.Board;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class MapCheckTests
    {
        [Test]
        public void PlayableMaps_HaveNoProblems()
        {
            CollectionAssert.IsEmpty(Problems(TestMaps.Lobby));
            CollectionAssert.IsEmpty(Problems(TestMaps.Small));
        }

        [Test]
        public void NoEntrance()
        {
            AssertProblem("no entrance", "MMMWWW", "MmMWwW", "......", "LLLLLL", "######");
        }

        [Test]
        public void NoDoor()
        {
            AssertProblem("no men's door", "MMMWWW", "MMMWwW", "......", "LLLLLL", "##EE##");
        }

        [Test]
        public void TwoDoors()
        {
            AssertProblem("2 men's doors", "MMMWWW", "mmMWwW", "......", "LLLLLL", "##EE##");
        }

        [Test]
        public void BuildingThatIsNotARectangle()
        {
            AssertProblem("men's building (M) is not a filled rectangle", "MM.WWW", "MmMWwW", "......", "LLLLLL", "##EE##");
        }

        [Test]
        public void DoorWithoutBuilding()
        {
            AssertProblem("no men's building", "...WWW", ".m.WwW", "......", "LLLLLL", "##EE##");
        }

        [Test]
        public void DoorInsideItsBuilding()
        {
            AssertProblem("opens onto 0", "MMMWWW", "MmMWwW", "MMM...", "LLLLLL", "##EE##");
        }

        [Test]
        public void DoorOpeningTwoWays()
        {
            AssertProblem("opens onto 2", "MMM.WW", "MMm.Ww", "......", "LLLLLL", "##EE##");
        }

        [Test]
        public void DoorsSideBySide()
        {
            AssertProblem("side by side", "MMMWWW", "MMmwWW", "......", "LLLLLL", "##EE##");
        }

        [Test]
        public void DoorThatCannotBeReached()
        {
            AssertProblem("men's door (m) cannot be reached", "MMMWWW", "MmMWwW", "#.#...", "###LLL", "##EE##");
        }

        [Test]
        public void DoorFatCharactersCannotReach()
        {
            AssertProblem("Fat characters cannot reach the men's door", "MMMWWW", "MmMWwW", "#n#...", "#.#LLL", "#.EE##");
        }

        [Test]
        public void NoLobby()
        {
            AssertProblem("No lobby tile", "MMMWWW", "MmMWwW", "......", "......", "##EE##");
        }

        [Test]
        public void LobbyThatNeedsAStepAcrossTheSideOfANarrowTile()
        {
            // The entrance's only way on is sideways into a narrow tile, through its post.
            AssertProblem("No lobby tile", "MMMWWW", "MmMWwW", "......", "#LLLLL", "En####");
        }

        [Test]
        public void LobbyFatCharactersCannotReach()
        {
            AssertProblem("Fat characters cannot reach a lobby tile", "MMMWWW", "MmMWwW", "......", "LLLLLL", "##nn##",
                "##EE##");
        }

        private static IReadOnlyList<string> Problems(params string[] rows) => MapCheck.Problems(MapParser.Parse(rows));

        private static void AssertProblem(string expected, params string[] rows)
        {
            var problems = Problems(rows);
            Assert.That(problems, Has.Some.Contains(expected), string.Join("\n", problems));
        }
    }
}
