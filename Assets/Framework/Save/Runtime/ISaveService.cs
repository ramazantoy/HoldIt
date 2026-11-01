using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace AF.Save
{
    /// <summary>
    /// Loads and writes save documents. Global and device data are loaded during boot (await <c>IAppBoot.WaitReady</c>).
    /// Slot data lives in a session: <see cref="LoadSlot"/> or <see cref="NewSlot"/> opens one, <see cref="SaveSlot()"/>
    /// writes it, <see cref="CloseSlot"/> ends it. Slot operations fail with <see cref="InvalidOperationException"/> while a
    /// load is still running.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>How the global document was loaded during boot; <see cref="LoadStatus.None"/> before that.</summary>
        LoadStatus GlobalStatus { get; }

        /// <summary>How the device document was loaded during boot; <see cref="LoadStatus.None"/> before that.</summary>
        LoadStatus DeviceStatus { get; }

        /// <summary>True while slot data is in memory: a loaded or new slot, or a dev session.</summary>
        bool HasSession { get; }

        /// <summary>
        /// The slot that <see cref="SaveSlot()"/> and autosaves write: the one last loaded, started or saved. Null without a
        /// session and in a dev session.
        /// </summary>
        string ActiveSlot { get; }

        /// <summary>
        /// Gameplay was entered without a slot (Play pressed in a gameplay scene): the session lives in memory and is never written.
        /// </summary>
        bool IsDevSession { get; }

        /// <summary>Time spent in gameplay, unpaused, in this session; saved with the slot.</summary>
        TimeSpan PlayTime { get; }

        /// <summary>Every slot in the storage, most recently saved first. Damaged and too-new slots are listed with their status.</summary>
        UniTask<IReadOnlyList<SlotInfo>> ListSlots(CancellationToken cancellation = default);

        /// <summary>
        /// Reads a slot and, if it is usable, makes it the session: every registered slot savable is restored. On any other
        /// status the current session stays as it was.
        /// </summary>
        UniTask<LoadResult> LoadSlot(string slotId, CancellationToken cancellation = default);

        /// <summary>
        /// Starts a new game in a slot: every registered slot savable gets its default. Nothing is written until a save, but the
        /// first save (an autosave on suspend included) replaces whatever the slot held, whatever its status. Check
        /// <see cref="ListSlots"/> and ask the player before starting over an existing slot.
        /// </summary>
        void NewSlot(string slotId);

        /// <summary>Captures the session and writes it to <see cref="ActiveSlot"/>.</summary>
        UniTask<SaveResult> SaveSlot(CancellationToken cancellation = default);

        /// <summary>
        /// Captures the session and writes it to <paramref name="slotId"/>, which becomes the active slot ("save as"). Replaces
        /// what that slot held, like <see cref="NewSlot"/>. Cancelling stops the wait only: a write that reaches the storage
        /// still makes the slot active.
        /// </summary>
        UniTask<SaveResult> SaveSlot(string slotId, CancellationToken cancellation = default);

        /// <summary>Ends the session without writing (back to the main menu). Savables keep their state until the next load.</summary>
        void CloseSlot();

        /// <summary>Deletes every copy of a slot, the quarantined <c>.corrupt</c> one included. Deleting the active slot also closes the session.</summary>
        UniTask<SaveResult> DeleteSlot(string slotId, CancellationToken cancellation = default);

        /// <summary>Captures and writes the global and device documents.</summary>
        UniTask<SaveResult> SaveGlobal(CancellationToken cancellation = default);
    }
}
