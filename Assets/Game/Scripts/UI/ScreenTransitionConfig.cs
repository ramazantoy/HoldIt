using UnityEngine;

namespace HoldIt.UI
{
    /// <summary>How screens fade in and out (<see cref="ScreenView"/>). One asset, shared by every screen.</summary>
    [CreateAssetMenu(menuName = "HoldIt/UI/Screen Transition Config", fileName = "ScreenTransitionConfig")]
    public sealed class ScreenTransitionConfig : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("Seconds a screen takes to fade in or out. 0 shows and hides at once.")]
        private float _fadeSeconds = 0.2f;

        public float FadeSeconds => _fadeSeconds;
    }
}
