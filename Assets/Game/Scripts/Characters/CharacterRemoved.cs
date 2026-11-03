namespace HoldIt.Characters
{
    /// <summary>A character left the board (into a stall); <see cref="ICharacters"/> no longer has it.</summary>
    public readonly struct CharacterRemoved
    {
        public readonly CharacterId Id;

        public CharacterRemoved(CharacterId id) => Id = id;
    }
}
