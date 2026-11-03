using System;
using HoldIt.Levels;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// Keeps the HUD up to date with the level's run: it reads the level, the lives and who made it once at the start, then
    /// follows their changes.
    /// </summary>
    internal sealed class HudPresenter : IInitializable, IStartable, IDisposable
    {
        private readonly IGameplayHudView _hud;
        private readonly ILevelRun _run;
        private readonly ILevelCursor _cursor;
        private readonly ISubscriber<ServedChanged> _servedChanged;
        private readonly ISubscriber<LivesChanged> _livesChanged;
        private IDisposable _subscriptions;

        public HudPresenter(IGameplayHudView hud, ILevelRun run, ILevelCursor cursor,
            ISubscriber<ServedChanged> servedChanged, ISubscriber<LivesChanged> livesChanged)
        {
            _hud = hud;
            _run = run;
            _cursor = cursor;
            _servedChanged = servedChanged;
            _livesChanged = livesChanged;
        }

        public void Initialize()
        {
            var bag = DisposableBag.CreateBuilder();
            _servedChanged.Subscribe(message => _hud.ShowServed(message.Served, _run.Crowd)).AddTo(bag);
            _livesChanged.Subscribe(message => _hud.ShowLives(message.Lives, _run.MaxLives)).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Start()
        {
            _hud.ShowLevel(_cursor.Number);
            _hud.ShowServed(_run.Served, _run.Crowd);
            _hud.ShowLives(_run.Lives, _run.MaxLives);
        }

        public void Dispose() => _subscriptions?.Dispose();
    }
}
