using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace AF.Core
{
    [SceneLoadExempt("The scene loader's own calls to Unity.")]
    internal sealed class UnitySceneOperations : ISceneOperations
    {
        private const string TRANSITION_SCENE = "Scene Transition";

        // AsyncOperation.progress stops here until the scene activates.
        private const float LoadedProgress = 0.9f;

        public string ActiveScene => SceneManager.GetActiveScene().name;

        public bool IsInBuild(string scene) => Application.CanStreamedLevelBeLoaded(scene);

        public bool IsLoaded(string scene) => SceneManager.GetSceneByName(scene).isLoaded;

        public async UniTask UnloadAll(CancellationToken cancellation)
        {
            // Unity never unloads the last scene; the empty one stays until the Single load replaces it. It may already exist
            // when an earlier change failed halfway.
            var transition = SceneManager.GetSceneByName(TRANSITION_SCENE);
            if (!transition.IsValid())
                transition = SceneManager.CreateScene(TRANSITION_SCENE);
            SceneManager.SetActiveScene(transition);

            // Newest first: additive scenes, whose scopes are children of the main scene's scope, go before their parent and
            // never run a frame against its disposed services.
            var others = new List<Scene>();
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != transition && scene.isLoaded)
                    others.Add(scene);
            }
            foreach (var scene in others)
                await Wait(SceneManager.UnloadSceneAsync(scene), scene.name, null, cancellation);
        }

        public UniTask LoadSingle(string scene, Action<float> progress, CancellationToken cancellation) =>
            Wait(SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single), scene, progress, cancellation);

        public async UniTask LoadAdditive(string scene, string mainScene, CancellationToken cancellation)
        {
            var parent = LifetimeScope.Find<SceneScope>(SceneManager.GetSceneByName(mainScene));
            if (parent == null)
            {
                // Without a scope in the main scene the new scope attaches to the root, as every scene scope does by default.
                await Wait(SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive), scene, null, cancellation);
                return;
            }

            // Until the load ends, every scope that wakes up without a parent of its own gets this one; the new scene's scope
            // is the one meant. A scope created elsewhere during those frames (a prefab with its own scope) would get it too.
            using (LifetimeScope.EnqueueParent(parent))
                await Wait(SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive), scene, null, cancellation);
        }

        public UniTask Unload(string scene, CancellationToken cancellation) =>
            Wait(SceneManager.UnloadSceneAsync(scene), scene, null, cancellation);

        private static async UniTask Wait(AsyncOperation operation, string scene, Action<float> progress, CancellationToken cancellation)
        {
            if (operation == null)
            {
                // Unity also refuses while the app quits or Play stops; the loader has cancelled the token then.
                cancellation.ThrowIfCancellationRequested();
                throw new InvalidOperationException($"Unity did not start loading or unloading the scene '{scene}'.");
            }
            while (!operation.isDone)
            {
                progress?.Invoke(Mathf.Clamp01(operation.progress / LoadedProgress));
                await UniTask.Yield(PlayerLoopTiming.Update, cancellation);
            }
            progress?.Invoke(1f);
        }
    }
}
