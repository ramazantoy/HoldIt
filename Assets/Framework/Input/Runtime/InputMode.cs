using System;
using System.Collections.Generic;
using System.Linq;

namespace AF.Input
{
    /// <summary>What the mouse cursor does while a mode is active. The input mode service is the only writer of <c>UnityEngine.Cursor</c>.</summary>
    public enum CursorState
    {
        /// <summary>Visible and free to move. Hidden while the player uses a control scheme without a mouse (gamepad, touch).</summary>
        Free,

        /// <summary>Hidden, not locked: cutscenes, loading.</summary>
        Hidden,

        /// <summary>Hidden and locked to the centre of the window: mouse look.</summary>
        Locked,
    }

    /// <summary>
    /// The action maps that are enabled together, and what the cursor does meanwhile. Modes are data: the game defines its
    /// modes once, in one static class, and requests one from <see cref="IInputModeService"/>. Every map a mode does not list
    /// is disabled while it is active. There are no always-on maps; a map that every mode needs is listed in each.
    /// </summary>
    public sealed class InputMode
    {
        /// <summary>No map enabled. Active during Boot.</summary>
        public static readonly InputMode None = new InputMode("none", CursorState.Free);

        private readonly string[] _maps;

        public InputMode(string name, CursorState cursor, params string[] maps)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("An input mode needs a name.", nameof(name));
            if (maps != null && maps.Any(string.IsNullOrEmpty))
                throw new ArgumentException($"Input mode '{name}' lists an empty map name.", nameof(maps));

            Name = name;
            Cursor = cursor;
            _maps = maps?.Distinct(StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
        }

        public string Name { get; }
        public CursorState Cursor { get; }

        /// <summary>Names of the action maps enabled in this mode.</summary>
        public IReadOnlyList<string> Maps => _maps;

        public bool Enables(string map) => Array.IndexOf(_maps, map) >= 0;

        public override string ToString() => Name;
    }
}
