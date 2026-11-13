namespace AF.Save
{
    /// <summary>Passed to <c>builder.UseSave(options)</c> in the root scope.</summary>
    public sealed class SaveOptions
    {
        public SaveSchema Slot { get; set; } = new SaveSchema();
        public SaveSchema Global { get; set; } = new SaveSchema();
        public SaveSchema Device { get; set; } = new SaveSchema();

        /// <summary>Where documents are kept. Null: files under <c>Application.persistentDataPath</c>.</summary>
        public ISaveStorage Storage { get; set; }

        /// <summary>
        /// When the app goes to the background or quits, also write the active slot if it changed since it was last written.
        /// Global and device data are always written then if they changed. Turn off for checkpoint-only games.
        /// </summary>
        public bool AutosaveSlotOnSuspend { get; set; } = true;

        /// <summary>
        /// Whether the game keeps slots. Off for a game whose progress lives in global data only: gameplay then starts without
        /// a dev session (and without its error log in a build), and every slot operation throws. Global and device data are
        /// unaffected.
        /// </summary>
        public bool UsesSlots { get; set; } = true;

        internal SaveSchema SchemaOf(SaveDocument document) => document switch
        {
            SaveDocument.Global => Global,
            SaveDocument.Device => Device,
            _ => Slot,
        };
    }
}
