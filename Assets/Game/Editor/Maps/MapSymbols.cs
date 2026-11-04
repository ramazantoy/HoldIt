using HoldIt.Board;

namespace HoldIt.Editor.Maps
{
    /// <summary>The characters a map is written in, as <see cref="MapParser"/> reads them.</summary>
    public static class MapSymbols
    {
        public const char Floor = '.';
        public const char Lobby = 'L';
        public const char Entrance = 'E';
        public const char Wall = '#';
        public const char Narrow = 'n';
        public const char MaleBuilding = 'M';
        public const char FemaleBuilding = 'W';
        public const char MaleDoor = 'm';
        public const char FemaleDoor = 'w';

        /// <summary>Every symbol, in the order an editor palette shows them.</summary>
        public static readonly char[] All =
            { Floor, Lobby, Entrance, Wall, Narrow, MaleBuilding, MaleDoor, FemaleBuilding, FemaleDoor };

        public static char Building(Gender gender) => gender == Gender.Male ? MaleBuilding : FemaleBuilding;
        public static char Door(Gender gender) => gender == Gender.Male ? MaleDoor : FemaleDoor;

        /// <summary>True for a tile characters can stand on: everything but walls and buildings.</summary>
        public static bool IsWalkable(char symbol) =>
            symbol != Wall && symbol != MaleBuilding && symbol != FemaleBuilding;
    }
}
