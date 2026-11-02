namespace HoldIt.Tests
{
    /// <summary>Maps the board tests share.</summary>
    internal static class TestMaps
    {
        /// <summary>Map 1 (Lobby), as in its asset.</summary>
        public static readonly string[] Lobby =
        {
            "MMMMMWWWWW",
            "MMMMMWWWWW",
            "MMMMMWWWWW",
            "MMmMMWWwWW",
            "..........",
            ".##....##.",
            ".##.##.##.",
            "..........",
            ".###nn###.",
            "..........",
            "LLLLLLLLLL",
            "LLLLLLLLLL",
            "#LLLLLLLL#",
            "####EE####",
        };

        /// <summary>The smallest playable map: two 3 x 2 restrooms opening south, a floor row, a lobby row, two entrances.</summary>
        public static readonly string[] Small =
        {
            "MMMWWW",
            "MmMWwW",
            "......",
            "LLLLLL",
            "##EE##",
        };
    }
}
