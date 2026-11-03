namespace HoldIt.Restrooms
{
    /// <summary>A stall's state changed; <see cref="IRestrooms.StateOf"/> already gives <see cref="State"/>.</summary>
    public readonly struct StallStateChanged
    {
        public readonly Gender Gender;
        public readonly int Stall;
        public readonly StallState State;

        public StallStateChanged(Gender gender, int stall, StallState state)
        {
            Gender = gender;
            Stall = stall;
            State = state;
        }
    }
}
