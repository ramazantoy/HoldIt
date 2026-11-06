using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Feedback;
using HoldIt.Levels;
using HoldIt.Restrooms;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class FeedbackEventsTests
    {
        private RecordingFeedback _feedback;
        private Broker<StepRefused> _refused;
        private Broker<StallStateChanged> _stall;
        private Broker<ServedChanged> _served;
        private Broker<LivesChanged> _lives;
        private Broker<CharacterSpawned> _spawned;
        private Broker<LevelEnded> _ended;
        private FeedbackEvents _events;

        [SetUp]
        public void SetUpEvents()
        {
            _feedback = new RecordingFeedback();
            _refused = new Broker<StepRefused>();
            _stall = new Broker<StallStateChanged>();
            _served = new Broker<ServedChanged>();
            _lives = new Broker<LivesChanged>();
            _spawned = new Broker<CharacterSpawned>();
            _ended = new Broker<LevelEnded>();
            _events = new FeedbackEvents(_feedback, _refused.Subscriber, _stall.Subscriber, _served.Subscriber,
                _lives.Subscriber, _spawned.Subscriber, _ended.Subscriber);
            _events.Initialize();
        }

        [TearDown]
        public void TearDownEvents()
        {
            _events.Dispose();
            _refused.Dispose();
            _stall.Dispose();
            _served.Dispose();
            _lives.Dispose();
            _spawned.Dispose();
            _ended.Dispose();
        }

        [Test]
        public void StepRefused_ForTheRules_IsRefused()
        {
            _refused.Publish(new StepRefused(new CharacterId(1), StepRefusal.WrongDoor));
            _refused.Publish(new StepRefused(new CharacterId(1), StepRefusal.TooWide));

            CollectionAssert.AreEqual(new[] { FeedbackKind.Refused, FeedbackKind.Refused }, _feedback.Played);
        }

        [Test]
        public void StepRefused_ForAnOccupiedCell_IsBlocked()
        {
            _refused.Publish(new StepRefused(new CharacterId(1), StepRefusal.Blocked));

            CollectionAssert.AreEqual(new[] { FeedbackKind.Blocked }, _feedback.Played);
        }

        [Test]
        public void StepRefused_WithNoReason_IsSilent()
        {
            _refused.Publish(new StepRefused(new CharacterId(1), StepRefusal.None));

            Assert.IsEmpty(_feedback.Played);
        }

        [Test]
        public void Stall_OnlyShuttingIsHeard()
        {
            _stall.Publish(new StallStateChanged(Gender.Male, 0, StallState.Taken));
            _stall.Publish(new StallStateChanged(Gender.Male, 0, StallState.Free));
            Assert.IsEmpty(_feedback.Played);

            _stall.Publish(new StallStateChanged(Gender.Male, 0, StallState.Shut));

            CollectionAssert.AreEqual(new[] { FeedbackKind.StallShut }, _feedback.Played);
        }

        [Test]
        public void ServedLivesAndArrivals_EachHaveTheirKind()
        {
            _served.Publish(new ServedChanged(1));
            _lives.Publish(new LivesChanged(2));
            _spawned.Publish(new CharacterSpawned(new CharacterId(3), Gender.Female, BodySize.Thin, new Cell(1, 1)));

            CollectionAssert.AreEqual(new[] { FeedbackKind.Served, FeedbackKind.Accident, FeedbackKind.Spawn }, _feedback.Played);
        }

        [Test]
        public void LevelEnded_WonAndLostSoundDifferent_AndPlayingIsSilent()
        {
            _ended.Publish(new LevelEnded(LevelOutcome.Playing, 0, 0));
            Assert.IsEmpty(_feedback.Played);

            _ended.Publish(new LevelEnded(LevelOutcome.Won, 3, 6));
            _ended.Publish(new LevelEnded(LevelOutcome.Lost, 0, 2));

            CollectionAssert.AreEqual(new[] { FeedbackKind.LevelWon, FeedbackKind.LevelLost }, _feedback.Played);
        }

        [Test]
        public void Dispose_StopsListening()
        {
            _events.Dispose();

            _served.Publish(new ServedChanged(1));

            Assert.IsEmpty(_feedback.Played);
        }

        private sealed class RecordingFeedback : IFeedback
        {
            public List<FeedbackKind> Played { get; } = new List<FeedbackKind>();

            public void Play(FeedbackKind kind) => Played.Add(kind);
        }
    }
}
