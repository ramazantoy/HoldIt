using System;
using HoldIt.Characters;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Levels
{
    /// <summary>
    /// Keeps the score of the level being played. A character that goes into its stall is served
    /// (<see cref="CharacterStateChanged"/> InStall); an accident costs a life (<see cref="CharacterRemoved"/> with
    /// <see cref="RemovalReason.Accident"/>), and the last life loses the level. Once everyone the level brings has come in
    /// (<see cref="CharacterSpawned"/>) and the board is empty again, the level is won, with a star for every life left.
    /// Nothing counts once it has ended.
    /// </summary>
    internal sealed class LevelRun : ILevelRun, IInitializable, IDisposable
    {
        private readonly LevelDefinition _level;
        private readonly ICharacters _characters;
        private readonly ISubscriber<CharacterSpawned> _spawned;
        private readonly ISubscriber<CharacterStateChanged> _stateChanged;
        private readonly ISubscriber<CharacterRemoved> _removed;
        private readonly IPublisher<ServedChanged> _servedChanged;
        private readonly IPublisher<LivesChanged> _livesChanged;
        private readonly IPublisher<LevelEnded> _ended;
        private IDisposable _subscriptions;
        private int _arrived;

        public LevelRun(LevelDefinition level, RunConfig config, ICharacters characters,
            ISubscriber<CharacterSpawned> spawned, ISubscriber<CharacterStateChanged> stateChanged,
            ISubscriber<CharacterRemoved> removed, IPublisher<ServedChanged> servedChanged,
            IPublisher<LivesChanged> livesChanged, IPublisher<LevelEnded> ended)
        {
            _level = level;
            _characters = characters;
            _spawned = spawned;
            _stateChanged = stateChanged;
            _removed = removed;
            _servedChanged = servedChanged;
            _livesChanged = livesChanged;
            _ended = ended;
            MaxLives = config.Lives;
            Lives = config.Lives;
        }

        public int Crowd => _level.Crowd;
        public int Served { get; private set; }
        public int Lives { get; private set; }
        public int MaxLives { get; }
        public LevelOutcome Outcome { get; private set; } = LevelOutcome.Playing;
        public int Stars { get; private set; }

        public void Initialize()
        {
            var bag = DisposableBag.CreateBuilder();
            _spawned.Subscribe(_ => Arrive()).AddTo(bag);
            _stateChanged.Subscribe(Serve).AddTo(bag);
            _removed.Subscribe(Leave).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Dispose() => _subscriptions?.Dispose();

        private void Arrive()
        {
            if (Outcome == LevelOutcome.Playing)
                _arrived++;
        }

        private void Serve(CharacterStateChanged message)
        {
            if (Outcome != LevelOutcome.Playing || message.State != CharacterState.InStall)
                return;
            Served++;
            _servedChanged.Publish(new ServedChanged(Served));
        }

        private void Leave(CharacterRemoved message)
        {
            if (Outcome != LevelOutcome.Playing)
                return;

            if (message.Reason == RemovalReason.Accident)
            {
                Lives--;
                _livesChanged.Publish(new LivesChanged(Lives));
                if (Lives <= 0)
                {
                    End(LevelOutcome.Lost);
                    return;
                }
            }

            if (_arrived >= _level.Crowd && _characters.All.Count == 0)
                End(LevelOutcome.Won);
        }

        private void End(LevelOutcome outcome)
        {
            Outcome = outcome;
            Stars = outcome == LevelOutcome.Won ? Lives : 0;
            _ended.Publish(new LevelEnded(outcome, Stars, Served));
        }
    }
}
