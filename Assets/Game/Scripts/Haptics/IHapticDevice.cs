namespace HoldIt.Haptics
{
    /// <summary>The phone vibrator, or something that stands in for it. It does what it is told; the settings are checked before.</summary>
    internal interface IHapticDevice
    {
        void Vibrate(HapticStrength strength);
    }
}
