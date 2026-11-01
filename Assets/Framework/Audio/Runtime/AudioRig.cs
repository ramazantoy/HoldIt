using System;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace AF.Audio
{
    /// <summary>The <c>Audio</c> object under the root scope that holds the listener, the music sources and the voice template.</summary>
    internal sealed class AudioRig : IDisposable
    {
        private const string NAME = "Audio";

        [Inject]
        public AudioRig(IObjectResolver resolver) : this((resolver.ApplicationOrigin as Component)?.transform)
        {
        }

        // Tests pass no scope.
        internal AudioRig(Transform parent)
        {
            Root = new GameObject(NAME).transform;
            if (parent != null)
                Root.SetParent(parent, false);
        }

        public Transform Root { get; }

        public Transform CreateChild(string name, bool active = true)
        {
            var child = new GameObject(name);
            child.SetActive(active);
            child.transform.SetParent(Root, false);
            return child.transform;
        }

        public void Dispose()
        {
            if (Root == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(Root.gameObject);
            else
                Object.DestroyImmediate(Root.gameObject);
        }
    }
}
