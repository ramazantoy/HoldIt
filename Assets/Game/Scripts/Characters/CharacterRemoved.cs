namespace HoldIt.Characters
{
    /// <summary>A character left the board, into its stall or after an accident; <see cref="ICharacters"/> no longer has it.</summary>
    public readonly struct CharacterRemoved
    {
        public readonly CharacterId Id;
        public readonly RemovalReason Reason;

        public CharacterRemoved(CharacterId id, RemovalReason reason)
        {
            Id = id;
            Reason = reason;
        }
    }
}
