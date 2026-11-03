using System;

namespace HoldIt.Restrooms
{
    /// <summary>
    /// One stall's round: taken, then shut once its visitor is in, then free again when its time since it was taken is up.
    /// It never frees before the visitor is in.
    /// </summary>
    internal sealed class Stall
    {
        private float _elapsed;

        public StallState State { get; private set; } = StallState.Free;

        public void Take()
        {
            if (State != StallState.Free)
                throw new InvalidOperationException($"Only a free stall can be taken; this one is {State}.");
            State = StallState.Taken;
            _elapsed = 0f;
        }

        public void Shut()
        {
            if (State != StallState.Taken)
                throw new InvalidOperationException($"Only a taken stall can be shut; this one is {State}.");
            State = StallState.Shut;
        }

        /// <summary>Lets <paramref name="deltaTime"/> pass. True when that freed the stall.</summary>
        public bool Advance(float deltaTime, float busySeconds)
        {
            if (State == StallState.Free)
                return false;

            _elapsed += deltaTime;
            if (State != StallState.Shut || _elapsed < busySeconds)
                return false;

            State = StallState.Free;
            return true;
        }
    }
}
