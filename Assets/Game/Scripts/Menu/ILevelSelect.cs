using System;

namespace HoldIt.Menu
{
    /// <summary>The level select. <see cref="MenuFlow"/> opens it, and plays the level chosen.</summary>
    public interface ILevelSelect
    {
        bool IsOpen { get; }

        event Action Closed;

        /// <summary>An open level was chosen: its index, from 0.</summary>
        event Action<int> LevelChosen;

        /// <summary>Shows every level as the progress has it now. Does nothing while open.</summary>
        void Open();

        /// <summary>Does nothing while closed.</summary>
        void Close();
    }
}
