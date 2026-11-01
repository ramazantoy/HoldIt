using UnityEngine.InputSystem;

namespace AF.Input
{
    /// <summary>
    /// The control scheme the player is using, from the devices that perform the asset's actions. UI that shows button prompts
    /// reads <see cref="Current"/> once when it starts, then follows <see cref="ControlSchemeChanged"/>.
    /// </summary>
    public interface IControlSchemeTracker
    {
        /// <summary>
        /// Name of a control scheme of the actions asset. Before any input, the first scheme whose devices are all connected;
        /// null when none is.
        /// </summary>
        string Current { get; }

        /// <summary>
        /// The device that last performed an action, or null. Devices created by scripts (on-screen controls, a virtual mouse)
        /// are never reported: they stand in for the device the player really uses.
        /// </summary>
        InputDevice Device { get; }

        /// <summary>True when the current scheme uses a mouse, so a free cursor is shown. True while no scheme is known.</summary>
        bool UsesMouse { get; }
    }
}
