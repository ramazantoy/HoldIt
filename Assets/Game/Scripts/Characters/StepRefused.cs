namespace HoldIt.Characters
{
    /// <summary>
    /// A character gave up a step, for <see cref="Reason"/>. It already walks back or stands where it stopped; its
    /// <see cref="CharacterStateChanged"/> comes first.
    /// </summary>
    public readonly struct StepRefused
    {
        public readonly CharacterId Id;
        public readonly StepRefusal Reason;

        public StepRefused(CharacterId id, StepRefusal reason)
        {
            Id = id;
            Reason = reason;
        }
    }
}
