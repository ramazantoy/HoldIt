using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Gameplay
{
    /// <summary>A level that stands: its scope. Disposing it takes everything of the level down.</summary>
    internal interface ILevel : IDisposable
    {
        /// <summary>
        /// Adds the transform of everything the level has on the board right now (characters, marks, puddles, puffs) and in
        /// the world UI layer (bubbles, "+1"), so the board can take them out with it.
        /// </summary>
        void CollectContent(List<Transform> into);
    }
}
