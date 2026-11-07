using HoldIt.Backdrop;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class BackdropTransitionTests
    {
        private static readonly BackdropColors _black = new BackdropColors(Color.black, Color.black);
        private static readonly BackdropColors _white = new BackdropColors(Color.white, Color.white);

        [Test]
        public void Duration_NoChange_IsTheMinimum() =>
            Assert.That(BackdropTransition.Duration(_white, _white, 1f, 3f, 0.5f), Is.EqualTo(1f).Within(1e-4f));

        [Test]
        public void Duration_FarApart_IsTheMaximum() =>
            Assert.That(BackdropTransition.Duration(_black, _white, 1f, 3f, 0.5f), Is.EqualTo(3f).Within(1e-4f));

        [Test]
        public void Duration_HalfWay_IsBetween()
        {
            var near = new BackdropColors(new Color(0.1f, 0f, 0f), new Color(0.1f, 0f, 0f));
            var seconds = BackdropTransition.Duration(_black, near, 1f, 3f, 0.2f);
            Assert.That(seconds, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void Duration_WithoutFullDistance_IsTheMaximum() =>
            Assert.That(BackdropTransition.Duration(_black, _black, 1f, 3f, 0f), Is.EqualTo(3f));

        [Test]
        public void Lerp_AtTheEnds_GivesTheColours()
        {
            Assert.That(BackdropColors.Lerp(_black, _white, 0f).Top, Is.EqualTo(Color.black));
            Assert.That(BackdropColors.Lerp(_black, _white, 1f).Bottom, Is.EqualTo(Color.white));
        }
    }
}
