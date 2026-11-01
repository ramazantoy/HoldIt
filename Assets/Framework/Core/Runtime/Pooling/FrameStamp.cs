using UnityEngine;

namespace AF.Core
{
    /// <summary>When something happened, in frames and in fixed steps. Tests pass their own clock.</summary>
    internal readonly struct FrameStamp
    {
        public readonly int Frame;
        public readonly float FixedTime;

        public FrameStamp(int frame, float fixedTime)
        {
            Frame = frame;
            FixedTime = fixedTime;
        }

        public static FrameStamp Now() => new FrameStamp(Time.frameCount, Time.fixedTime);
    }
}
