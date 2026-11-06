using UnityEngine;

namespace HoldIt.Haptics
{
    /// <summary>How the phone vibrates.</summary>
    [CreateAssetMenu(menuName = "HoldIt/Haptics/Haptics Config", fileName = "HapticsConfig")]
    public sealed class HapticsConfig : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("The shortest time between two vibrations, in game seconds; one asked for sooner is dropped.")]
        private float _minGap = 0.05f;

        [SerializeField, Tooltip("Android 8 and 9 only: a tick. From Android 10 the system tick effect is used.")]
        private HapticPulse _tick = new HapticPulse(10, 60);

        [SerializeField, Tooltip("Android 8 and 9 only: a light click.")]
        private HapticPulse _light = new HapticPulse(20, 120);

        [SerializeField, Tooltip("Android 8 and 9 only: a heavy click.")]
        private HapticPulse _heavy = new HapticPulse(40, 255);

        public float MinGap => _minGap;

        public HapticPulse Pulse(HapticStrength strength) => strength switch
        {
            HapticStrength.Tick => _tick,
            HapticStrength.Light => _light,
            _ => _heavy,
        };
    }
}
