using UnityEngine;
using VContainer.Unity;

namespace AF.Audio
{
    // Moves in PostLateUpdate, after cameras and their controllers moved in LateUpdate.
    internal sealed class ListenerFollower : IAudioListener, IPostLateTickable
    {
        private const string NAME = "Listener";

        private readonly Transform _listener;
        private Transform _target;

        public ListenerFollower(AudioRig rig)
        {
            _listener = rig.CreateChild(NAME);
            _listener.gameObject.AddComponent<AudioListener>();
        }

        public void Follow(Transform target) => _target = target;

        public void PostLateTick()
        {
            var target = _target;
            if (target == null)
            {
                var camera = Camera.main;
                if (camera == null)
                    return;
                target = camera.transform;
            }
            _listener.SetPositionAndRotation(target.position, target.rotation);
        }
    }
}
