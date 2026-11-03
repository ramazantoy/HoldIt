using HoldIt.Board;
using HoldIt.Characters;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class StepRulesTests
    {
        [Test]
        public void Check_TheOtherGendersDoor_IsTheWrongDoor()
        {
            Assert.AreEqual(StepVerdict.WrongDoor,
                StepRules.Check(new Tile(TileType.Door, Gender.Male), Gender.Female, BodySize.Thin));
            Assert.AreEqual(StepVerdict.WrongDoor,
                StepRules.Check(new Tile(TileType.Door, Gender.Female), Gender.Male, BodySize.Normal));
        }

        [Test]
        public void Check_TheirOwnDoor_LetsThemIn()
        {
            Assert.AreEqual(StepVerdict.Enter,
                StepRules.Check(new Tile(TileType.Door, Gender.Male), Gender.Male, BodySize.Fat));
            Assert.AreEqual(StepVerdict.Enter,
                StepRules.Check(new Tile(TileType.Door, Gender.Female), Gender.Female, BodySize.Thin));
        }

        [Test]
        public void Check_ANarrowPassage_IsTooNarrowOnlyForFat()
        {
            var narrow = new Tile(TileType.Narrow);

            Assert.AreEqual(StepVerdict.TooWide, StepRules.Check(narrow, Gender.Male, BodySize.Fat));
            Assert.AreEqual(StepVerdict.TooWide, StepRules.Check(narrow, Gender.Female, BodySize.Fat));
            Assert.AreEqual(StepVerdict.Enter, StepRules.Check(narrow, Gender.Male, BodySize.Normal));
            Assert.AreEqual(StepVerdict.Enter, StepRules.Check(narrow, Gender.Female, BodySize.Thin));
        }

        [Test]
        public void Check_OpenFloor_LetsEveryoneIn()
        {
            foreach (var type in new[] { TileType.Floor, TileType.Lobby, TileType.Entrance })
            {
                Assert.AreEqual(StepVerdict.Enter, StepRules.Check(new Tile(type), Gender.Male, BodySize.Fat), type.ToString());
                Assert.AreEqual(StepVerdict.Enter, StepRules.Check(new Tile(type), Gender.Female, BodySize.Thin), type.ToString());
            }
        }
    }
}
