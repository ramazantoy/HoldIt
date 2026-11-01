using UnityEngine;
using UnityEngine.Localization;

namespace AF.Localization.Tests
{
    /// <summary>Holds localized references for the validator tests; created in memory only.</summary>
    internal sealed class LocalizedReferenceFixture : ScriptableObject
    {
        public LocalizedString found = new LocalizedString("UI", "play");
        public LocalizedString missingEntry = new LocalizedString("UI", "quit");
        public LocalizedString missingTable = new LocalizedString("Dialogue", "play");
        public LocalizedString unset = new LocalizedString();
        public LocalizedString[] list = { new LocalizedString { TableReference = "UI" } };
    }
}
