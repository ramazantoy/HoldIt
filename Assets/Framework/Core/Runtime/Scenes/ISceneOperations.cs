using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace AF.Core
{
    /// <summary>The Unity side of <see cref="SceneLoader"/>; tests replace it.</summary>
    internal interface ISceneOperations
    {
        string ActiveScene { get; }

        bool IsInBuild(string scene);

        bool IsLoaded(string scene);

        /// <summary>Makes an empty scene active, then unloads every other scene and waits until their objects are destroyed.</summary>
        UniTask UnloadAll(CancellationToken cancellation);

        /// <summary>Loads the scene in Single mode (it is active from its first Awake), reporting progress from 0 to 1.</summary>
        UniTask LoadSingle(string scene, Action<float> progress, CancellationToken cancellation);

        /// <summary>Loads the scene additively, its scope a child of <paramref name="mainScene"/>'s scope.</summary>
        UniTask LoadAdditive(string scene, string mainScene, CancellationToken cancellation);

        UniTask Unload(string scene, CancellationToken cancellation);
    }
}
