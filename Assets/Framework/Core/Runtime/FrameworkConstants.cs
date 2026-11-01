namespace AF.Core
{
    /// <summary>
    /// The only place where the framework prefix appears.
    /// Persistent data (save keys, PlayerPrefs keys, file names) must never use these values.
    /// </summary>
    public static class FrameworkConstants
    {
        public const string Prefix = "AF";
        public const string MenuRoot = Prefix + "/";

        // Persistent names. They never derive from Prefix: renaming the framework must not orphan players' saves.

        /// <summary>Folder under persistentDataPath for slots and global data. Cloud sync (Steam Auto-Cloud) covers it.</summary>
        public const string SaveFolder = "saves";

        /// <summary>Folder under persistentDataPath for data that belongs to this device only. Cloud sync must not cover it.</summary>
        public const string DeviceSaveFolder = "local";

        public const string GlobalSaveName = "global";
        public const string DeviceSaveName = "device";
        public const string SlotSavePrefix = "slot-";
        public const string SaveFileExtension = ".json";

        /// <summary>PlayerPrefs cannot enumerate its keys; the PlayerPrefs save storage keeps its entry list under this key.</summary>
        public const string SaveIndexKey = "saves-index";
    }
}
