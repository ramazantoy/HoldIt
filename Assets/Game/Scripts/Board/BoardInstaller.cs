using VContainer;
using VContainer.Unity;

namespace HoldIt.Board
{
    public static class BoardInstaller
    {
        /// <summary>
        /// In the gameplay scene's scope, which outlasts a level: the board's view and the camera that fits whatever board
        /// stands in it.
        /// </summary>
        public static void UseBoardFrame(this IContainerBuilder builder, BoardView view, BoardCamera camera)
        {
            builder.RegisterComponent(view).As<IBoardBounds>();
            builder.RegisterComponent(camera).As<IBoardPicker, IBoardProjection>();
        }

        /// <summary>
        /// In a level's scope: the map's grid, and the board built from it in the scene's view for as long as the level
        /// lasts. The map must parse; "AF/Validate All" reports one that does not.
        /// </summary>
        public static void UseBoard(this IContainerBuilder builder, MapDefinition map)
        {
            builder.RegisterInstance<ITileGrid>(MapParser.Parse(map.Rows));
            builder.RegisterEntryPoint<BoardBuilder>();
        }
    }
}
