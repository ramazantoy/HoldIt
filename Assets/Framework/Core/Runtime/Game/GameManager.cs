using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;

namespace AF.Core
{
    public sealed class GameManager : IGameManager, IDisposable
    {
        private readonly Dictionary<Type, IGameState> _states;
        private readonly IPublisher<GameStateChanged> _stateChanged;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private bool _disposed;

        public GameManager(IReadOnlyList<IGameState> states, IPublisher<GameStateChanged> stateChanged)
        {
            _states = new Dictionary<Type, IGameState>(states.Count);
            foreach (var state in states)
            {
                if (!_states.TryAdd(state.GetType(), state))
                    throw new InvalidOperationException($"Game state {state.GetType().Name} is registered more than once.");
            }

            _stateChanged = stateChanged;
            Current = Get(typeof(BootState));
        }

        public IGameState Current { get; private set; }
        public bool IsTransitioning { get; private set; }

        public async UniTask GoTo<TState>() where TState : IGameState
        {
            var next = Get(typeof(TState));
            if (IsTransitioning)
                throw new InvalidOperationException($"Cannot go to {next} while the transition from {Current} is still running.");
            if (next == Current)
                return;

            var previous = Current;
            var lifetime = _lifetime.Token;
            IsTransitioning = true;
            try
            {
                await previous.Exit(lifetime);
            }
            catch
            {
                // Still in the previous state; subscribers have nothing to follow.
                IsTransitioning = false;
                throw;
            }

            Current = next;
            try
            {
                await next.Enter(lifetime);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
                // The manager is disposed with the root scope; nobody is left to notify.
                IsTransitioning = false;
                throw;
            }
            catch
            {
                // Current is already the next state: subscribers (input mode, save) must follow it although Enter failed. The
                // caller still gets the exception.
                IsTransitioning = false;
                Publish(previous, next, " (Enter failed)");
                throw;
            }

            IsTransitioning = false;
            Publish(previous, next, "");
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        private void Publish(IGameState previous, IGameState next, string note)
        {
            Log.Info($"Game state: {previous} -> {next}{note}");
            _stateChanged.Publish(new GameStateChanged(previous, next));
        }

        private IGameState Get(Type type)
        {
            if (_states.TryGetValue(type, out var state))
                return state;
            throw new InvalidOperationException(
                $"Game state {type.Name} is not registered. Register it with builder.Register<{type.Name}>(Lifetime.Singleton).As<IGameState>().");
        }
    }
}
