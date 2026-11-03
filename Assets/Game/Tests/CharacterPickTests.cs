using HoldIt.Characters;
using HoldIt.Drawing;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class CharacterPickTests
    {
        private static readonly CharacterId _near = new CharacterId(1);
        private static readonly CharacterId _far = new CharacterId(2);

        [Test]
        public void TryPick_OnTheHead_PicksTheCharacter()
        {
            var candidates = new[] { new PickCandidate(_near, new Vector2(100, 100), new Vector2(100, 160), 30f) };

            Assert.IsTrue(CharacterPick.TryPick(candidates, new Vector2(104, 158), out var id));
            Assert.AreEqual(_near, id);
        }

        [Test]
        public void TryPick_BesideTheBodyWithinTheRadius_PicksTheCharacter()
        {
            var candidates = new[] { new PickCandidate(_near, new Vector2(100, 100), new Vector2(100, 160), 30f) };

            Assert.IsTrue(CharacterPick.TryPick(candidates, new Vector2(128, 130), out _));
        }

        [Test]
        public void TryPick_PastTheRadius_PicksNobody()
        {
            var candidates = new[] { new PickCandidate(_near, new Vector2(100, 100), new Vector2(100, 160), 30f) };

            Assert.IsFalse(CharacterPick.TryPick(candidates, new Vector2(140, 130), out _));
            Assert.IsFalse(CharacterPick.TryPick(candidates, new Vector2(100, 200), out _), "Above the head.");
            Assert.IsFalse(CharacterPick.TryPick(candidates, new Vector2(100, 60), out _), "Below the feet.");
        }

        [Test]
        public void TryPick_BetweenTwo_PicksTheNearerLine()
        {
            var candidates = new[]
            {
                new PickCandidate(_far, new Vector2(100, 100), new Vector2(100, 160), 50f),
                new PickCandidate(_near, new Vector2(160, 100), new Vector2(160, 160), 50f),
            };

            Assert.IsTrue(CharacterPick.TryPick(candidates, new Vector2(135, 120), out var id));
            Assert.AreEqual(_near, id);
        }

        [Test]
        public void TryPick_NoCandidates_PicksNobody()
        {
            Assert.IsFalse(CharacterPick.TryPick(new PickCandidate[0], Vector2.zero, out _));
        }

        [Test]
        public void TryPick_AFlatCandidate_MeasuresToItsFeet()
        {
            var candidates = new[] { new PickCandidate(_near, new Vector2(100, 100), new Vector2(100, 100), 10f) };

            Assert.IsTrue(CharacterPick.TryPick(candidates, new Vector2(106, 106), out _));
            Assert.IsFalse(CharacterPick.TryPick(candidates, new Vector2(110, 110), out _));
        }
    }
}
