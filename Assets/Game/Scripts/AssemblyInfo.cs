using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Game.Tests")]
// The Editor's debug menus (HoldIt/Progress) reach the game's internal debug hooks.
[assembly: InternalsVisibleTo("Game.Editor")]
