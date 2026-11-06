using System.Text.RegularExpressions;
using HoldIt.Feedback;
using HoldIt.Haptics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HoldIt.Tests
{
    public class FeedbackTableTests
    {
        [Test]
        public void TryFire_AKindWithoutAnEntry_DoesNothing()
        {
            var table = new FeedbackTable(new[] { new FeedbackEntry(FeedbackKind.Served) });

            Assert.IsFalse(table.TryFire(FeedbackKind.Accident, 0f, out var entry));
            Assert.IsNull(entry);
        }

        [Test]
        public void TryFire_AListedKind_GivesItsEntry()
        {
            var served = new FeedbackEntry(FeedbackKind.Served, haptic: HapticStrength.Light);
            var table = new FeedbackTable(new[] { new FeedbackEntry(FeedbackKind.Accident), served });

            Assert.IsTrue(table.TryFire(FeedbackKind.Served, 0f, out var entry));
            Assert.AreSame(served, entry);
            Assert.AreEqual(HapticStrength.Light, entry.Haptic);
        }

        [Test]
        public void TryFire_AnEntryWithoutACue_StillFires()
        {
            var table = new FeedbackTable(new[] { new FeedbackEntry(FeedbackKind.Accident, haptic: HapticStrength.Heavy) });

            Assert.IsTrue(table.TryFire(FeedbackKind.Accident, 5f, out var entry));
            Assert.IsNull(entry.Cue);
        }

        [Test]
        public void TryFire_AgainWithinTheInterval_IsDropped()
        {
            var table = new FeedbackTable(new[] { new FeedbackEntry(FeedbackKind.PathCell, minInterval: 0.04f) });

            Assert.IsTrue(table.TryFire(FeedbackKind.PathCell, 1f, out _));
            Assert.IsFalse(table.TryFire(FeedbackKind.PathCell, 1.03f, out _));
        }

        [Test]
        public void TryFire_AfterTheInterval_FiresAgain()
        {
            var table = new FeedbackTable(new[] { new FeedbackEntry(FeedbackKind.PathCell, minInterval: 0.04f) });
            table.TryFire(FeedbackKind.PathCell, 1f, out _);

            Assert.IsTrue(table.TryFire(FeedbackKind.PathCell, 1.05f, out _));
        }

        [Test]
        public void TryFire_ADroppedCall_DoesNotRestartTheInterval()
        {
            var table = new FeedbackTable(new[] { new FeedbackEntry(FeedbackKind.PathCell, minInterval: 0.1f) });
            table.TryFire(FeedbackKind.PathCell, 1f, out _);
            table.TryFire(FeedbackKind.PathCell, 1.08f, out _);

            Assert.IsTrue(table.TryFire(FeedbackKind.PathCell, 1.11f, out _));
        }

        [Test]
        public void TryFire_NoInterval_AlwaysFires()
        {
            var table = new FeedbackTable(new[] { new FeedbackEntry(FeedbackKind.Served) });

            Assert.IsTrue(table.TryFire(FeedbackKind.Served, 2f, out _));
            Assert.IsTrue(table.TryFire(FeedbackKind.Served, 2f, out _));
        }

        [Test]
        public void TryFire_TheFirstCallAtTimeZero_Fires()
        {
            var table = new FeedbackTable(new[] { new FeedbackEntry(FeedbackKind.Spawn, minInterval: 0.15f) });

            Assert.IsTrue(table.TryFire(FeedbackKind.Spawn, 0f, out _));
        }

        [Test]
        public void TryFire_KindsKeepTheirOwnIntervals()
        {
            var table = new FeedbackTable(new[]
            {
                new FeedbackEntry(FeedbackKind.Spawn, minInterval: 1f),
                new FeedbackEntry(FeedbackKind.Served, minInterval: 1f),
            });
            table.TryFire(FeedbackKind.Spawn, 1f, out _);

            Assert.IsTrue(table.TryFire(FeedbackKind.Served, 1.1f, out _));
        }

        [Test]
        public void Constructor_AKindListedTwice_KeepsTheFirstAndSaysSo()
        {
            LogAssert.Expect(LogType.Error, new Regex("Served twice"));
            var first = new FeedbackEntry(FeedbackKind.Served, haptic: HapticStrength.Light);
            var second = new FeedbackEntry(FeedbackKind.Served, haptic: HapticStrength.Heavy);

            var table = new FeedbackTable(new[] { first, second });

            table.TryFire(FeedbackKind.Served, 0f, out var entry);
            Assert.AreSame(first, entry);
        }

        [Test]
        public void Constructor_AMissingEntry_IsSkipped()
        {
            var table = new FeedbackTable(new FeedbackEntry[] { null, new FeedbackEntry(FeedbackKind.Served) });

            Assert.IsTrue(table.TryFire(FeedbackKind.Served, 0f, out _));
        }
    }
}
