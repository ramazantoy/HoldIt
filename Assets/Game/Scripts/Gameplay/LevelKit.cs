using HoldIt.Backdrop;
using HoldIt.Characters;
using HoldIt.Drawing;
using HoldIt.Levels;
using HoldIt.Restrooms;
using HoldIt.Spawning;

namespace HoldIt.Gameplay
{
    /// <summary>The assets a level's scope is built from; the gameplay scene holds them, every level uses the same ones.</summary>
    internal sealed class LevelKit
    {
        public LevelKit(RestroomConfig restroomConfig, StallSet stallSet, CharacterConfig characterConfig,
            CharacterSet characterSet, RunConfig runConfig, SpawnConfig spawnConfig, DrawingConfig drawingConfig,
            BackdropConfig backdropConfig)
        {
            RestroomConfig = restroomConfig;
            StallSet = stallSet;
            CharacterConfig = characterConfig;
            CharacterSet = characterSet;
            RunConfig = runConfig;
            SpawnConfig = spawnConfig;
            DrawingConfig = drawingConfig;
            BackdropConfig = backdropConfig;
        }

        public RestroomConfig RestroomConfig { get; }
        public StallSet StallSet { get; }
        public CharacterConfig CharacterConfig { get; }
        public CharacterSet CharacterSet { get; }
        public RunConfig RunConfig { get; }
        public SpawnConfig SpawnConfig { get; }
        public DrawingConfig DrawingConfig { get; }
        public BackdropConfig BackdropConfig { get; }
    }
}
