using System;
using System.Collections.Generic;
using AF.Core;
using AF.Input;
using Cysharp.Threading.Tasks;
using HoldIt.Input;
using HoldIt.Levels;
using HoldIt.Paths;
using HoldIt.Restrooms;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace HoldIt.Tests
{
    /// <summary>Records the scene changes asked for, as "State Scene" or "Scene".</summary>
    internal sealed class FakeSceneLoader : ISceneLoader
    {
        public readonly List<string> Loads = new List<string>();

        public string Current => "Test";
        public bool IsLoading => false;

        public UniTask Load(string scene)
        {
            Loads.Add(scene);
            return UniTask.CompletedTask;
        }

        public UniTask Load<TState>(string scene) where TState : IGameState
        {
            Loads.Add($"{typeof(TState).Name} {scene}");
            return UniTask.CompletedTask;
        }

        public UniTask LoadAdditive(string scene) => throw new NotSupportedException();
        public UniTask UnloadAdditive(string scene) => throw new NotSupportedException();
    }

    /// <summary>Game time the test sets by hand.</summary>
    internal sealed class FakeClock : IGameClock
    {
        public float Time { get; set; }
        public float DeltaTime { get; set; }
        public float Scale { get; set; } = 1f;
    }

    /// <summary>
    /// Restrooms whose stalls only change when told: taken by <see cref="TryTake"/> (each way in is a straight line
    /// <see cref="LineLength"/> long), shut by <see cref="Shut"/> and freed by <see cref="Free"/>. No layout.
    /// </summary>
    internal sealed class FakeRestrooms : IRestrooms
    {
        private readonly StallState[,] _states = new StallState[2, 2];

        public readonly List<(Gender Gender, int Stall)> Shuts = new List<(Gender, int)>();

        public float LineLength { get; set; } = 2f;
        public int StallCount => 2;

        public StallState StateOf(Gender gender, int stall) => _states[(int)gender, stall];

        public RestroomLayout LayoutOf(Gender gender) => throw new NotSupportedException();

        public bool TryTake(Gender gender, out StallVisit visit)
        {
            for (var i = 0; i < StallCount; i++)
            {
                if (_states[(int)gender, i] != StallState.Free)
                    continue;
                _states[(int)gender, i] = StallState.Taken;
                visit = new StallVisit(gender, i, new WalkLine(new[] { Vector3.zero, Vector3.forward * LineLength }));
                return true;
            }
            visit = null;
            return false;
        }

        public void Shut(Gender gender, int stall)
        {
            if (_states[(int)gender, stall] != StallState.Taken)
                throw new InvalidOperationException($"Stall {gender} {stall} is {_states[(int)gender, stall]}.");
            _states[(int)gender, stall] = StallState.Shut;
            Shuts.Add((gender, stall));
        }

        /// <summary>Takes every stall of the gender, as busy visitors would.</summary>
        public void Fill(Gender gender)
        {
            for (var i = 0; i < StallCount; i++)
                _states[(int)gender, i] = StallState.Shut;
        }

        public void Free(Gender gender, int stall) => _states[(int)gender, stall] = StallState.Free;
    }

    /// <summary>A real MessagePipe broker for <see cref="BackPressed"/>, as the root scope registers it.</summary>
    internal sealed class BackBroker : IDisposable
    {
        private readonly IObjectResolver _container;

        public BackBroker()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<BackPressed>(messagePipe);
            _container = builder.Build();
            Subscriber = _container.Resolve<ISubscriber<BackPressed>>();
        }

        public ISubscriber<BackPressed> Subscriber { get; }

        public void Press() => _container.Resolve<IPublisher<BackPressed>>().Publish(new BackPressed());

        public void Dispose() => _container.Dispose();
    }

    /// <summary>A real MessagePipe broker for one message type, as a scope registers it.</summary>
    internal sealed class Broker<T> : IDisposable
    {
        private readonly IObjectResolver _container;

        public Broker()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<T>(messagePipe);
            _container = builder.Build();
            Subscriber = _container.Resolve<ISubscriber<T>>();
            Publisher = _container.Resolve<IPublisher<T>>();
        }

        public ISubscriber<T> Subscriber { get; }
        public IPublisher<T> Publisher { get; }

        public void Publish(T message) => Publisher.Publish(message);

        public void Dispose() => _container.Dispose();
    }

    /// <summary>Pause requests, kept until their handles are disposed.</summary>
    internal sealed class FakePause : IPauseService
    {
        private readonly List<string> _reasons = new List<string>();

        public bool IsPaused => _reasons.Count > 0;
        public IReadOnlyList<string> ActiveReasons => _reasons;

        public IDisposable Request(string reason)
        {
            _reasons.Add(reason);
            return new Handle(() => _reasons.Remove(reason));
        }

        private sealed class Handle : IDisposable
        {
            private Action _release;

            public Handle(Action release) => _release = release;

            public void Dispose()
            {
                _release?.Invoke();
                _release = null;
            }
        }
    }

    /// <summary>Input mode requests, kept until their handles are disposed; the newest is current.</summary>
    internal sealed class FakeInputModes : IInputModeService
    {
        private readonly List<InputMode> _requests = new List<InputMode>();

        public InputMode Current => _requests.Count > 0 ? _requests[_requests.Count - 1] : InputModes.Gameplay;
        public IReadOnlyList<InputMode> Requests => _requests;
        public bool ConfineCursor { get; set; }

        public IDisposable Request(InputMode mode)
        {
            _requests.Add(mode);
            return new Release(() => _requests.Remove(mode));
        }

        private sealed class Release : IDisposable
        {
            private Action _release;

            public Release(Action release) => _release = release;

            public void Dispose()
            {
                _release?.Invoke();
                _release = null;
            }
        }
    }

    /// <summary>A level run the test sets by hand.</summary>
    internal sealed class FakeLevelRun : ILevelRun
    {
        public int Crowd { get; set; } = 10;
        public int Served { get; set; }
        public int Lives { get; set; } = 3;
        public int MaxLives { get; set; } = 3;
        public LevelOutcome Outcome { get; set; } = LevelOutcome.Playing;
        public int Stars { get; set; }
    }

    /// <summary>A cursor over <see cref="Count"/> levels without definitions.</summary>
    internal sealed class FakeLevelCursor : ILevelCursor
    {
        public int Index { get; set; }
        public int Number => Index + 1;
        public int Count { get; set; } = 3;
        public LevelDefinition Current => null;
        public bool HasNext => Index < Count - 1;

        public void Next()
        {
            if (!HasNext)
                throw new InvalidOperationException("No next level.");
            Index++;
        }
    }
}
