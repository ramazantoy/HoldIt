using HoldIt.Board;

namespace HoldIt.Characters
{
    /// <summary>A character was added to the board, standing Idle on <see cref="Cell"/>.</summary>
    public readonly struct CharacterSpawned
    {
        public readonly CharacterId Id;
        public readonly Gender Gender;
        public readonly BodySize Size;
        public readonly Cell Cell;

        public CharacterSpawned(CharacterId id, Gender gender, BodySize size, Cell cell)
        {
            Id = id;
            Gender = gender;
            Size = size;
            Cell = cell;
        }
    }
}
