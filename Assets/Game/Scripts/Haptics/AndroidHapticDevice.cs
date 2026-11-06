using System;
using AF.Core;
using UnityEngine;

namespace HoldIt.Haptics
{
    /// <summary>
    /// The Android vibrator, through <c>android.os.VibrationEffect</c>. Android 10 and up play the system tick and click
    /// effects; Android 8 and 9 a one-shot buzz of the config's length and strength; older ones only the heavy click, with
    /// <c>Handheld.Vibrate</c> (which also makes Unity add the VIBRATE permission to the manifest). The Java objects are
    /// found on the first use. A call that fails is logged once and the device goes quiet.
    /// </summary>
    internal sealed class AndroidHapticDevice : IHapticDevice
    {
        private const int EffectClick = 0;
        private const int EffectTick = 2;
        private const int EffectHeavyClick = 5;
        private const int PredefinedSince = 29;
        private const int OneShotSince = 26;

        private readonly HapticsConfig _config;
        private AndroidJavaObject _vibrator;
        private AndroidJavaClass _effects;
        private int _sdk;
        private bool _ready;
        private bool _failed;

        public AndroidHapticDevice(HapticsConfig config) => _config = config;

        public void Vibrate(HapticStrength strength)
        {
            if (_failed)
                return;
            try
            {
                if (!_ready)
                    Connect();
                if (_vibrator == null)
                    return;

                if (_sdk >= PredefinedSince)
                {
                    Play(_effects.CallStatic<AndroidJavaObject>("createPredefined", EffectOf(strength)));
                }
                else if (_sdk >= OneShotSince)
                {
                    var pulse = _config.Pulse(strength);
                    Play(_effects.CallStatic<AndroidJavaObject>("createOneShot", (long)pulse.Milliseconds, pulse.Amplitude));
                }
                else if (strength == HapticStrength.Heavy)
                {
                    Handheld.Vibrate();
                }
            }
            catch (Exception exception)
            {
                _failed = true;
                Log.Warning($"Vibration failed and is switched off: {exception.Message}");
            }
        }

        private void Play(AndroidJavaObject effect)
        {
            using (effect)
                _vibrator.Call("vibrate", effect);
        }

        private void Connect()
        {
            _ready = true;
            using var build = new AndroidJavaClass("android.os.Build$VERSION");
            _sdk = build.GetStatic<int>("SDK_INT");
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
            {
                vibrator?.Dispose();
                return;
            }
            _vibrator = vibrator;
            if (_sdk >= OneShotSince)
                _effects = new AndroidJavaClass("android.os.VibrationEffect");
        }

        private static int EffectOf(HapticStrength strength) => strength switch
        {
            HapticStrength.Tick => EffectTick,
            HapticStrength.Light => EffectClick,
            _ => EffectHeavyClick,
        };
    }
}
