using System;
using System.Collections.Generic;
using AF.Core;
using Cysharp.Threading.Tasks;
using HoldIt.Input;
using MessagePipe;
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
}
