using VContainer;
using VContainer.Unity;

namespace HoldIt.Board
{
    public static class BoardInstaller
    {
        /// <summary>
        /// Registers the map's grid and the board's view and camera in a scene scope. The map must parse; "AF/Validate All"
        /// reports one that does not.
        /// </summary>
        public static void UseBoard(this IContainerBuilder builder, MapDefinition map, BoardView view, BoardCamera camera)
        {
            builder.RegisterInstance<ITileGrid>(MapParser.Parse(map.Rows));
            builder.RegisterComponent(view).As<IBoardBounds>();
            builder.RegisterComponent(camera).As<IBoardPicker, IBoardProjection>();
        }
    }
}
