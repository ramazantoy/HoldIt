using AF.Core;

namespace HoldIt.Haptics
{
    /// <summary>Stands in for the vibrator where there is none (the Editor, desktop builds): it only logs.</summary>
    internal sealed class LogHapticDevice : IHapticDevice
    {
        public void Vibrate(HapticStrength strength) => Log.Info($"Haptic: {strength}");
    }
}
