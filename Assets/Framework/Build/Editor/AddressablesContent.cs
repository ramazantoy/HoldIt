using System;
using System.IO;
using System.Linq;
#if AF_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace AF.Build.Editor
{
    /// <summary>
    /// The Addressables content build, a step of the build menu when the Addressables package is installed (Unity
    /// Localization loads its tables through it). Compiled out without the package (<c>AF_ADDRESSABLES</c> comes from the
    /// assembly's version defines).
    /// </summary>
    internal static class AddressablesContent
    {
        private const string CONTENT_FOLDER = "aa";
        private const string RUNTIME_SETTINGS = "settings.json";

#if AF_ADDRESSABLES
        public static bool Installed => true;
        public static bool SettingsExist => AddressableAssetSettingsDefaultObject.Settings != null;

        /// <summary>True unless the content build is off for player builds; "use the preference" depends on each machine.</summary>
        public static bool BuildsWithPlayer =>
            AddressableAssetSettingsDefaultObject.Settings is AddressableAssetSettings settings &&
            settings.BuildAddressablesWithPlayerBuild != AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;

        /// <summary>Builds the content for the active target into the Library folder the player build copies it from.</summary>
        public static string Build(out bool succeeded)
        {
            AddressableAssetSettings.BuildPlayerContent(out var result);
            succeeded = result != null && string.IsNullOrEmpty(result.Error);
            if (result == null)
                return "Addressables content: the build returned no result.";
            return succeeded
                ? $"Addressables content: built in {result.Duration:0.0} s, {result.LocationCount} locations."
                : $"Addressables content: the build failed: {result.Error}";
        }
#else
        public static bool Installed => false;
        public static bool SettingsExist => false;
        public static bool BuildsWithPlayer => false;

        public static string Build(out bool succeeded) => throw new InvalidOperationException("The Addressables package is not installed.");
#endif

        /// <summary>True when a built player folder carries the content: <c>StreamingAssets/aa/settings.json</c>.</summary>
        public static bool IsIncluded(string outputFolder) =>
            Directory.GetFiles(outputFolder, RUNTIME_SETTINGS, SearchOption.AllDirectories)
                .Any(file => string.Equals(Path.GetFileName(Path.GetDirectoryName(file)), CONTENT_FOLDER, StringComparison.Ordinal));
    }
}
