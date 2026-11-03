namespace HoldIt.Characters
{
    /// <summary>A character's state changed; the character already holds <see cref="State"/>.</summary>
    public readonly struct CharacterStateChanged
    {
        public readonly CharacterId Id;
        public readonly CharacterState State;

        public CharacterStateChanged(CharacterId id, CharacterState state)
        {
            Id = id;
            State = state;
        }
    }
}
