using System;
using System.Collections.Generic;

namespace AF.Core
{
    /// <summary>
    /// Pause is requested, not toggled: every source (pause menu, focus loss, ads, dialogs) holds its own handle and the game
    /// stays paused while any handle is alive. Disposing one handle never resumes a pause someone else still wants.
    /// </summary>
    public interface IPauseService
    {
        bool IsPaused { get; }

        /// <summary>Reasons of the live requests, oldest first. Meant for debugging a game that will not resume.</summary>
        IReadOnlyList<string> ActiveReasons { get; }

        /// <summary>Pauses the game until the returned handle is disposed. Disposing twice is harmless.</summary>
        IDisposable Request(string reason);
    }
}
