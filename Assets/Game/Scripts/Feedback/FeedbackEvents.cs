using System;
using HoldIt.Characters;
using HoldIt.Levels;
using HoldIt.Restrooms;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Feedback
{
    /// <summary>
    /// Turns the gameplay messages into feedback: a refused step, a stall that shut, one more served, a life lost, a new
    /// arrival, the level won or lost. Events a view sees first hand (a knock, a path growing, a star popping) are played
    /// by the view.
    /// </summary>
    internal sealed class FeedbackEvents : IInitializable, IDisposable
    {
        private readonly IFeedback _feedback;
        private readonly ISubscriber<StepRefused> _refused;
        private readonly ISubscriber<StallStateChanged> _stall;
        private readonly ISubscriber<ServedChanged> _served;
        private readonly ISubscriber<LivesChanged> _lives;
        private readonly ISubscriber<CharacterSpawned> _spawned;
        private readonly ISubscriber<LevelEnded> _ended;
        private IDisposable _subscriptions;

        public FeedbackEvents(IFeedback feedback, ISubscriber<StepRefused> refused, ISubscriber<StallStateChanged> stall,
            ISubscriber<ServedChanged> served, ISubscriber<LivesChanged> lives, ISubscriber<CharacterSpawned> spawned,
            ISubscriber<LevelEnded> ended)
        {
            _feedback = feedback;
            _refused = refused;
            _stall = stall;
            _served = served;
            _lives = lives;
            _spawned = spawned;
            _ended = ended;
        }

        public void Initialize()
        {
            var bag = DisposableBag.CreateBuilder();
            _refused.Subscribe(message => Refused(message.Reason)).AddTo(bag);
            _stall.Subscribe(message =>
            {
                if (message.State == StallState.Shut)
                    _feedback.Play(FeedbackKind.StallShut);
            }).AddTo(bag);
            _served.Subscribe(_ => _feedback.Play(FeedbackKind.Served)).AddTo(bag);
            _lives.Subscribe(_ => _feedback.Play(FeedbackKind.Accident)).AddTo(bag);
            _spawned.Subscribe(_ => _feedback.Play(FeedbackKind.Spawn)).AddTo(bag);
            _ended.Subscribe(message => Ended(message.Outcome)).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Dispose() => _subscriptions?.Dispose();

        private void Refused(StepRefusal reason)
        {
            switch (reason)
            {
                case StepRefusal.WrongDoor:
                case StepRefusal.TooWide:
                    _feedback.Play(FeedbackKind.Refused);
                    break;
                case StepRefusal.Blocked:
                    _feedback.Play(FeedbackKind.Blocked);
                    break;
            }
        }

        private void Ended(LevelOutcome outcome)
        {
            if (outcome == LevelOutcome.Won)
                _feedback.Play(FeedbackKind.LevelWon);
            else if (outcome == LevelOutcome.Lost)
                _feedback.Play(FeedbackKind.LevelLost);
        }
    }
}
