using HoldIt.Board;
using HoldIt.Feedback;
using HoldIt.Pooling;
using HoldIt.Restrooms;
using HoldIt.UI;

namespace HoldIt.Characters
{
    /// <summary>
    /// What the views of a level need from the level and the scene. The views are pooled in the root and injected there,
    /// where none of this exists; <see cref="CharacterViews"/> hands it to a view as it spawns it, and the view lets go of it
    /// when it is despawned, so a level's objects are not held past the level.
    /// </summary>
    public sealed class LevelViewContext
    {
        public LevelViewContext(ICharacters characters, ITileGrid grid, IRestrooms restrooms, IBoardProjection projection,
            IWorldUiLayer layer, IFeedback feedback, ILevelPools pools)
        {
            Characters = characters;
            Grid = grid;
            Restrooms = restrooms;
            Projection = projection;
            Layer = layer;
            Feedback = feedback;
            Pools = pools;
        }

        public ICharacters Characters { get; }
        public ITileGrid Grid { get; }
        public IRestrooms Restrooms { get; }
        public IBoardProjection Projection { get; }
        public IWorldUiLayer Layer { get; }

        /// <summary>The scene's feedback: the root's own would end with nothing, the scene's sounds end with the scene.</summary>
        public IFeedback Feedback { get; }

        /// <summary>What a view spawns from (a speech bubble): the level's loan of the root's pools.</summary>
        public ILevelPools Pools { get; }
    }
}
