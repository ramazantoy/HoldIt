using UnityEngine;

namespace HoldIt.UI
{
    /// <summary>How screens fade in and out (<see cref="ScreenView"/>) and how buttons answer a press. One asset, shared by every screen.</summary>
    [CreateAssetMenu(menuName = "HoldIt/UI/Screen Transition Config", fileName = "ScreenTransitionConfig")]
    public sealed class ScreenTransitionConfig : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("Seconds a screen takes to fade in or out. 0 shows and hides at once.")]
        private float _fadeSeconds = 0.2f;

        [SerializeField, Range(0.5f, 1f), Tooltip("The size a screen's content starts at when it fades in, as a share of its own.")]
        private float _popScale = 0.94f;

        [SerializeField, Range(0.5f, 1f), Tooltip("The size a pressed button shrinks to, as a share of its own.")]
        private float _pressScale = 0.94f;

        [SerializeField, Min(0.01f), Tooltip("Seconds a button takes to shrink when pressed and to come back.")]
        private float _pressSeconds = 0.08f;

        public float FadeSeconds => _fadeSeconds;
        public float PopScale => _popScale;
        public float PressScale => _pressScale;
        public float PressSeconds => _pressSeconds;
    }
}
