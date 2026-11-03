using VContainer;
using VContainer.Unity;

namespace HoldIt.Drawing
{
    public static class DrawingInstaller
    {
        /// <summary>
        /// Registers path drawing (<see cref="IPathDrawing"/>) and its marks in a scene scope that already has the pointer
        /// input, the board and the characters.
        /// </summary>
        public static void UseDrawing(this IContainerBuilder builder, DrawingConfig config)
        {
            builder.RegisterInstance(config);
            builder.RegisterEntryPoint<PathDrawing>().As<IPathDrawing>();
            builder.RegisterEntryPoint<PathMarkers>();
        }
    }
}
