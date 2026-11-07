using System;
using System.Threading;
using AF.Core;
using AF.Input;
using Cysharp.Threading.Tasks;
using HoldIt.Levels;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// Plays the root's current level (<see cref="ILevelCursor"/>) in a scope of its own when the gameplay scene starts, and
    /// changes it for the next or the same one without loading the scene again: the old board goes out
    /// (<see cref="IBoardReveal"/>), its scope is disposed (its characters, subscriptions and pools go with it), a new
    /// one is built and its board comes in. The input is blocked and the game paused from the request until the new board
    /// is in, as a scene load would have it; the same holds as the first level comes in.
    /// </summary>
    internal sealed class LevelSession : ILevelSession, IStartable, IDisposable
    {
        private const string PAUSE_REASON = "level change";

        private readonly ILevelScopeFactory _factory;
        private readonly ILevelCursor _cursor;
        private readonly IInputModeService _input;
        private readonly IPauseService _pause;
        private readonly IBoardReveal _reveal;
        private readonly Func<CancellationToken, UniTask> _nextFrame;
        private readonly CancellationTokenSource _life = new CancellationTokenSource();
        private IDisposable _level;
        private bool _disposed;

        [Inject]
        public LevelSession(ILevelScopeFactory factory, ILevelCursor cursor, IInputModeService input, IPauseService pause,
            IBoardReveal reveal)
            : this(factory, cursor, input, pause, reveal, token => UniTask.NextFrame(token))
        {
        }

        // Tests pass their own frames.
        internal LevelSession(ILevelScopeFactory factory, ILevelCursor cursor, IInputModeService input,
            IPauseService pause, IBoardReveal reveal, Func<CancellationToken, UniTask> nextFrame)
        {
            _factory = factory;
            _cursor = cursor;
            _input = input;
            _pause = pause;
            _reveal = reveal;
            _nextFrame = nextFrame;
        }

        public bool IsChanging { get; private set; }

        /// <summary>True while a level's scope stands.</summary>
        public bool IsOpen => _level != null;

        // In Start, not Initialize: the scene's views set themselves up in Awake, after the scope is built.
        public void Start()
        {
            _level = _factory.Create(_cursor.Current);
            Run(false, false).Forget();
        }

        public UniTask Retry() => Change(false);

        public UniTask Next() => Change(true);

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _life.Cancel();
            _life.Dispose();
            _level?.Dispose();
            _level = null;
        }

        private UniTask Change(bool next)
        {
            if (IsChanging || _level == null || (next && !_cursor.IsNextOpen))
                return UniTask.CompletedTask;
            return Run(true, next);
        }

        // Opens the level that stands already (the scene's first), or replaces it with the next or the same one.
        private async UniTask Run(bool replace, bool next)
        {
            IsChanging = true;
            var token = _life.Token;
            try
            {
                using (_input.Request(InputModes.Blocked))
                using (_pause.Request(PAUSE_REASON))
                {
                    // Out of the click that asked, or for the level's entry points to start.
                    await _nextFrame(token);

                    if (replace)
                    {
                        await _reveal.Hide(token);

                        _level.Dispose();
                        _level = null;
                        if (next)
                            _cursor.Next();
                        _level = _factory.Create(_cursor.Current);

                        // The new level's entry points start.
                        await _nextFrame(token);
                    }

                    await _reveal.Show(token);
                }
            }
            finally
            {
                IsChanging = false;
            }
        }
    }
}
