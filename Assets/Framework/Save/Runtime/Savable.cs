using System;

namespace AF.Save
{
    /// <summary>Which file a savable's data goes to.</summary>
    public enum SaveDocument
    {
        /// <summary>One play-through, loaded and saved through <see cref="ISaveService"/> slots.</summary>
        Slot,

        /// <summary>Shared by every slot and synced between the player's devices: achievements, gameplay settings. Loaded during boot.</summary>
        Global,

        /// <summary>This device only, never synced: resolution, quality, audio output. Loaded during boot.</summary>
        Device,
    }

    /// <summary>
    /// Something whose state is saved, as one section of a <see cref="SaveDocument"/>. Derive from <see cref="Savable{TData}"/>
    /// rather than implementing this directly. Savables are plain C# services: register them as singletons with
    /// <c>AsImplementedInterfaces()</c> in the root scope or a scene scope, and the scope links them to the save registry.
    /// </summary>
    public interface ISavable
    {
        /// <summary>Section name in the save file: stable, unique within its document, lowercase. Never derived from a type name.</summary>
        string Key { get; }

        SaveDocument Document { get; }
        Type DataType { get; }

        object CaptureData();
        void RestoreData(object data);
        object CreateDefaultData();
    }

    /// <summary>
    /// Base class of a savable whose state is a <typeparamref name="TData"/> object. The data class holds plain values: public
    /// fields, a parameterless constructor, no Unity object references and no interface or abstract members (validation
    /// checks these). Enums are written as strings.
    /// </summary>
    public abstract class Savable<TData> : ISavable where TData : class, new()
    {
        public abstract string Key { get; }
        public virtual SaveDocument Document => SaveDocument.Slot;
        Type ISavable.DataType => typeof(TData);

        /// <summary>
        /// Returns the current state. Called on the main thread when saving, and when the scope that registered this savable is
        /// disposed (scene unload), so it reads only this object's own fields, never scene objects that may already be destroyed.
        /// The result is serialized at once; returning a live field is fine.
        /// </summary>
        protected abstract TData Capture();

        /// <summary>
        /// Replaces the current state. <paramref name="data"/> is never null and belongs to this savable. Called when a slot is
        /// loaded or started, when global data is loaded, and when this savable is registered while its document is loaded.
        /// It must not throw. Publish your own "changed" message here if something mirrors this state.
        /// </summary>
        protected abstract void Restore(TData data);

        /// <summary>State of a new game, used when the document has no section for <see cref="Key"/>.</summary>
        protected virtual TData CreateDefault() => new TData();

        object ISavable.CaptureData() => Capture();
        void ISavable.RestoreData(object data) => Restore((TData)data);
        object ISavable.CreateDefaultData() => CreateDefault();
    }
}
