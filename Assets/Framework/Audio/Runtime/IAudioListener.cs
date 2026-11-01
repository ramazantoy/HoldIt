using UnityEngine;

namespace AF.Audio
{
    /// <summary>
    /// The game's single AudioListener lives under the root scope, so sound stays audible while scenes change. It follows the
    /// main camera, or the target set here. Scenes have no AudioListener of their own.
    /// </summary>
    public interface IAudioListener
    {
        /// <summary>Follows <paramref name="target"/> (the player in a third-person game); null, or once it is destroyed, the main camera.</summary>
        void Follow(Transform target);
    }
}
