using AF.Core;
using HoldIt.Settings;

namespace HoldIt.Haptics
{
    /// <summary>
    /// Passes a vibration on to the device unless the player turned vibration off, it asks for none, or another one just
    /// went off (<see cref="HapticsConfig.MinGap"/>). Reads the setting each time, so switching it takes effect at once.
    /// </summary>
    internal sealed class HapticPlayer : IHaptics
    {
        private readonly IHapticDevice _device;
        private readonly IGameSettings _settings;
        private readonly HapticsConfig _config;
        private readonly IGameClock _clock;
        private float _last = float.NegativeInfinity;

        public HapticPlayer(IHapticDevice device, IGameSettings settings, HapticsConfig config, IGameClock clock)
        {
            _device = device;
            _settings = settings;
            _config = config;
            _clock = clock;
        }

        public void Play(HapticStrength strength)
        {
            if (strength == HapticStrength.None || !_settings.Haptics)
                return;
            var now = _clock.Time;
            if (now - _last < _config.MinGap)
                return;
            _last = now;
            _device.Vibrate(strength);
        }
    }
}
